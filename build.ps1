<#
.SYNOPSIS
Build OneMore full installer kit for the specified architecture, or default project builds.

.PARAMETER Architecture
Builds the installer kit for the specifies architecture: x86 (default), x64, ARM64, or All.

.PARAMETER Clean
Clean all projects in the solution, removing all bin and obj directories.
No build is performed.

.PARAMETER Fast
Build just the .csproj projects using default parameters:
OneMore, OneMorCalendar, OneMoreProtocolHandler, OneMoreSetupActions, and OneMoreTray.

.PARAMETER Prep
Run DisableOutOfProcBuild. This only needs to be run once on a machine, or after upgrading
or reinstalling Visual Studio. It is required to build installer kits from the command line.
No build is performed.

.PARAMETER VLog
Enable verbose logging for MSBuild. This is useful for debugging build issues.
#>

[CmdletBinding(SupportsShouldProcess = $true)]
param (
	[ValidateSet('x86','x64','ARM64','All')]
	[string] $Architecture = 'x86',
	[string] $Detect,
	[switch] $Clean,
	[switch] $Fast,
	[switch] $Prep,
	[switch] $VLog
	)

Begin
{
	# - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	# Helpers...

	function FindVisualStudio
	{
		$cmd = Get-Command devenv -ErrorAction:SilentlyContinue
		if ($cmd -ne $null)
		{
			$script:devenv = $cmd.Source
			$script:ideroot = Split-Path -Parent $devenv
			$script:vsregedit = Join-Path $ideroot 'VSRegEdit.exe'
			write-Verbose "... devenv found at $devenv"
			return $true
		}

		$0 = 'C:\Program Files\Microsoft Visual Studio\2022'
		if (FindVS $0) { return $true }

		$0 = 'C:\Program Files (x86)\Microsoft Visual Studio\2019'
		return FindVS $0
	}

	function FindVS
	{
		param($vsroot)
		$script:devenv = Join-Path $vsroot 'Professional\Common7\IDE\devenv.com'
		if (!(Test-Path $devenv))
		{
			$script:devenv = Join-Path $vsroot 'Enterprise\Common7\IDE\devenv.com'
			if (!(Test-Path $devenv))
			{
				$script:devenv = Join-Path $vsroot 'Community\Common7\IDE\devenv.com'
				if (!(Test-Path $devenv))
				{
					Write-Host "devenv not found in $vsroot" -ForegroundColor Yellow
					return $false
				}
			}
		}

		$script:ideroot = Split-Path -Parent $devenv
		$script:vsregedit = Join-Path $ideroot 'VSRegEdit.exe'
		write-Verbose "... devenv found at $devenv"
		return $true
	}

	# - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	# Single Commands...

	function CleanSolution
	{
		$pushpop = (!(Test-Path OneMore.sln))
		if ($pushpop) { Push-Location .. }
		CleanProject 'OneMore'
		CleanProject 'OneMoreCalendar'
		CleanProject 'OneMoreProtocolHandler'
		CleanProject 'OneMoreSetup'
		CleanProject 'OneMoreSetupActions'
		CleanProject 'OneMoreTray'
		if ($pushpop) { Pop-Location }
	}
	
	function CleanProject
	{
		param([string]$project)
		Push-Location $project
		try
		{
			Write-verbose "... cleaning $project"
			$progpref = $ProgressPreference
			$ProgressPreference = 'SilentlyContinue' 
			if (Test-Path bin) { Remove-Item bin -Recurse -Force -Confirm:$false | Out-Null }
			if (Test-Path obj) { Remove-Item obj -Recurse -Force -Confirm:$false | Out-Null }
			if (Test-Path Debug) { Remove-Item Debug -Recurse -Force -Confirm:$false | Out-Null }
			if (Test-Path Release) { Remove-Item Release -Recurse -Force -Confirm:$false | Out-Null }
			$ProgressPreference = $progpref
		}
		finally
		{
			Pop-Location
		}
	}

	function DisablPrepOutOfProcBuild
	{
		$0 = Join-Path $ideroot 'CommonExtensions\Microsoft\VSI\DisableOutOfProcBuild'
		if (Test-Path $0)
		{
			Push-Location $0
			if (Test-Path .\DisableOutOfProcBuild.exe) {
				.\DisableOutOfProcBuild.exe
			}
			Pop-Location
			Write-Host '... disabled out-of-proc builds; reboot is recommended'
			return
		}
		Write-Host "*** could not find $0\DisableOutOfProcBuild.exe" -ForegroundColor Yellow
	}

	function DetectArchitecture
	{
		param($dllpath)

		$dllpath = resolve-Path $dllpath
		if (-not (Test-Path $DllPath)) {
			Write-Error "File not found: $DllPath"
			return
		}

		# Open the file as a binary stream
		$stream = [System.IO.File]::OpenRead($DllPath)
		try {
			$reader = New-Object System.Reflection.PortableExecutable.PEReader $stream
			$machine = $reader.PEHeaders.CoffHeader.Machine

			switch ([int]$machine) {
				0x014c { "x86" }
				0x0200 { "Itanium" }
				0x8664 { "x64" }
				0x01c4 { "ARM" }
				0xaa64 { "ARM64" }
				default { "Unknown Architecture: $machine" }
			}
		} finally {
			$stream.Close()
		}
	}

	# - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	# Fast...

	function BuildFast
	{
		Write-Host "... fast build with default configs" -ForegroundColor Yellow

		NugetRestore 'OneMore'
		BuildProject 'OneMore'

		NugetRestore 'OneMoreTray'
		BuildProject 'OneMoreTray'

		NugetRestore 'OneMoreCalendar'
		BuildProject 'OneMoreCalendar'

		BuildProject 'OneMoreProtocolHandler'

		BuildProject 'OneMoreSetupActions'

		ReportArchitectures
	}

	function NugetRestore
	{
		param($name)
		Push-Location $name

		$cmd = "nuget restore .\$name.csproj"
		write-Host $cmd -ForegroundColor DarkGray
		nuget restore .\$name.csproj -solutiondirectory ..

		Pop-Location
	}

	function BuildProject
	{
		param($name)
		Push-Location $name

		# output file cannot exist before build
		if (Test-Path .\Debug\*)
		{
			Remove-Item .\Debug\*.* -Force -Confirm:$false
		}

		$cmd = "$devenv .\$name.csproj /project $name /projectconfig 'Debug|AnyCPU' /build"
		write-Host $cmd -ForegroundColor DarkGray
		. $devenv .\$name.csproj /project $name /projectconfig 'Debug|AnyCPU' /build

		Pop-Location
	}

	# - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	# Kit...

	function Build
	{
		param($arc)
		$script:Architecture = $arc

		CleanSolution
		RestoreSolution

		if (BuildSolution)
		{
			ReportArchitectures
			BuildKit
		}
	}

	function RestoreSolution
	{
		Write-Host "`n... restoring nuget packages" -ForegroundColor Cyan
		Write-Host

		NugetRestore 'OneMore'
		NugetRestore 'OneMoreTray'
		NugetRestore 'OneMoreCalendar'
	}

	function BuildSolution
	{
		Write-Host "`n... building $Architecture solution" -ForegroundColor Cyan
		Write-Host

		SetBuildVerbosity 4

		try
		{
			$log = "$($env:TEMP)\OneMoreBuild.log"
			if (Test-Path $log) { Remove-Item $log -Force -Confirm:$false }

			$cmd = "$devenv .\OneMore.sln /build 'Debug|$Architecture' /out '$log'"
			Write-Host $cmd -ForegroundColor DarkGray
			. $devenv .\OneMore.sln /build "Debug|$Architecture" /out $log
		}
		finally
		{
			SetBuildVerbosity 1
		}

		$succeeded = 0;
		$failed = 1

		Get-Content $env:TEMP\OneMoreBuild.log -ErrorAction SilentlyContinue | `
			where { $_ -match '== Build: (\d+) succeeded, (\d+) failed'} | `
			select -last 1 | `
			foreach {
				$succeeded = $matches[1]
				$failed = $matches[2]
				$color = $failed -eq 0 ? 'Green' : 'Red'
				write-Host "`n... build completed: $succeeded succeeded, $failed failed" -ForegroundColor $color
			}

		return [bool]($succeeded -gt 0 -and $failed -eq 0)
	}

	function SetBuildVerbosity
	{
		param($level)
		if ($VLog)
		{
			$desc = $level -eq 4 ? 'enabling' : 'disabling'
			Write-Host "... $desc MSBuild verbose logging" -ForegroundColor DarkYellow
			$cmd = ". '$vsregedit' set local HKCU General MSBuildLoggerVerbosity dword $level`n"
			write-Host $cmd -ForegroundColor DarkGray
			. $vsregedit set local HKCU General MSBuildLoggerVerbosity dword $level | Out-Null
		}
	}

	function ReportArchitectures
	{
		$arc = (DetectArchitecture .\OneMore\bin\$Architecture\Debug\River.OneMoreAddIn.dll)
		Write-Host "... OneMore: $arc" -ForegroundColor DarkGray

		ReportModuleArchitecture 'OneMoreCalendar'
		ReportModuleArchitecture 'OneMoreProtocolHandler'
		ReportModuleArchitecture 'OneMoreSetupActions'
		ReportModuleArchitecture 'OneMoreTray'
		Write-Host
	}

	function ReportModuleArchitecture
	{
		param($name)
		$arc = (DetectArchitecture .\$name\bin\Debug\$name.exe)
		Write-Host "... $name`: $arc" -ForegroundColor DarkGray
	}

	function BuildKit
	{
		Write-Host "`n... building $Architecture kit" -ForegroundColor Cyan
		Write-Host

		Push-Location OneMoreSetup
		$vdproj = Resolve-Path .\OneMoreSetup.vdproj

		PreserveVdproj $vdproj

		try
		{
			ConfigureSetupProject $vdproj

			$log = "$($env:TEMP)\OneMoreBuild.log"
			$cmd = "$devenv .\OneMoreSetup.vdproj /build 'Debug|$Architecture' /project Setup /out '$log'"
			write-Host $cmd -ForegroundColor DarkGray

			. $devenv .\OneMoreSetup.vdproj /build "Debug|$Architecture" /project Setup /out $env:TEMP\OneMorebuild.log

			if ($LASTEXITCODE -eq 0)
			{
				$0 = Get-ChildItem .\Debug\OneMore_*.msi | select -first 1
				if (Test-Path $0)
				{
					# move msi to Downloads for safe-keeping and to allow next Platform build
					$1 = "$home\Downloads\OneMore_$productVersion`_Setup$Architecture.msi"
					Move-Item $0 $1 -Force -Confirm:$false
					Write-Host "... $Architecture MSI copied to $1" -ForegroundColor DarkYellow

					if (Get-Command checksum -ErrorAction SilentlyContinue)
					{
						if (Test-Path $0)
						{
							$sum = (checksum -t sha256 $0)
							Write-Host "... $Architecture checksum: $sum" -ForegroundColor DarkYellow
						}
					}
				}
			}
		}
		finally
		{
			RestoreVdproj $vdproj
			Pop-Location
		}
	}

	function PreserveVdproj
	{
		param($vdproj)
		Write-Host '... preserving vdproj' -ForegroundColor DarkGray

		Write-Verbose '... restoring vdproj from git'
		git restore $vdproj

		Copy-Item $vdproj .\vdproj.tmp -Force -Confirm:$false
	}

	function RestoreVdproj
	{
		param($vdproj)
		Write-Host '... restoring vdproj' -ForegroundColor DarkGray
		$0 = (Resolve-Path .\vdproj.tmp)
		if (Test-Path $0)
		{
			Copy-Item $0 $vdproj -Force -Confirm:$false
			Remove-Item $0 -Force -Confirm:$false
		}
	}

	function ConfigureSetupProject
	{
		param($vdproj)
		$lines = @(Get-Content $vdproj)

		$script:productVersion = $lines | `
			where { $_ -match '"ProductVersion" = "8:(.+?)"' } | `
			foreach { $matches[1] }

		Write-Host
		Write-Host "... configuring vdproj for $Architecture build of $productVersion" -ForegroundColor Yellow

		'' | Out-File $vdproj -nonewline

		$lines | foreach `
		{
			if ($_ -match '"OutputFileName" = "')
			{
				# "OutputFilename" = "8:Debug\\OneMore_v_Setupx64.msi"
				$line = $_.Replace('OneMore_v_', "OneMore_$($productVersion)_")
				$line.Replace('x64', $Architecture) | Out-File $vdproj -Append
			}
			elseif ($_ -match '"DefaultLocation" = "')
			{
				# "DefaultLocation" = "8:[ProgramFilesFolder][Manufacturer]\\[ProductName]"
				if ($Architecture -ge 'x86') {
					$_.Replace('ProgramFiles64Folder', 'ProgramFilesFolder') | Out-File $vdproj -Append
				} else {
					$_.Replace('ProgramFilesFolder', 'ProgramFiles64Folder') | Out-File $vdproj -Append
				}
			}
			elseif ($_ -match '"TargetPlatform" = "')
			{
				# x86 -> "3:0"
				# x64 -> "3:1"
				if ($Architecture -ge 'x86') {
					'"TargetPlatform" = "3:0"' | Out-File $vdproj -Append
				} else {
					'"TargetPlatform" = "3:1"' | Out-File $vdproj -Append
				}
			}
			elseif (($_ -match '"Name" = "8:OneMoreSetupActions --install ') -or `
					($_ -match '"Arguments" = "8:--install '))
			{
				# "Name" = "8:OneMoreSetupActions --install --x86"
				# "Arguments" = "8:--install --x86"
				$_.Replace('x86', $Architecture) | Out-File $vdproj -Append
			}
			elseif ($_ -match '"SourcePath" = .*WebView2Loader\.dll"$')
			{
				if ($Architecture -ne 'x86')
				{
					$line = $_.Replace('x86', $Architecture)
					$line.Replace('x64', $Architecture) | Out-File $vdproj -Append
				}
				else
				{
					$_ | Out-File $vdproj -Append
				}
			}
			elseif ($_ -match '"SourcePath" = .*SQLite.Interop\.dll"$')
			{
				if ($Architecture -eq 'x64')
				{
					$_.Replace('x86', 'x64') | Out-File $vdproj -Append
				}
				elseif ($Architecture -eq 'ARM64')
				{
					$line = $_.Replace('bin\\x86', 'bin\\ARM64')
					$line.Replace('Debug\\x86', 'Debug\\x64') | Out-File $vdproj -Append
				}
				else
				{
					$_ | Out-File $vdproj -Append
				}
			}
			elseif ($_ -notmatch '^"Scc')
			{
				$_ | Out-File $vdproj -Append
			}
		}
	}
}
Process
{
	$script:verboseColor = $PSStyle.Formatting.Verbose
	$PSStyle.Formatting.Verbose = $PSStyle.Foreground.BrightBlack

	if (-not (FindVisualStudio)) { return }

	if ($Detect) { DetectArchitecture $Detect; return }

	if ($Prep) { DisablPrepOutOfProcBuild; return }
	if ($Clean) { CleanSolution; return }

	if ($Fast) { BuildFast; return }

	if ($Architecture -eq 'All')
	{
		Build 'ARM64'
		Build 'x64'
		Build 'x86'
	}
	else
	{
		if ($Architecture -eq 'arm64') { $Architecture = 'ARM64' }

		Build $Architecture
	}
}
End
{
	$PSStyle.Formatting.Verbose = $verboseColor
}
