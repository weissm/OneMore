# 
# script for automatic merging of multiple branches
#
import logging, datetime, argparse, sys
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger('')

# parse parameter
parser = argparse.ArgumentParser("Parse options")
parser.add_argument('-t', '--target', help='Target branch', default="improveMD37")
parser.add_argument('-d', '--debug', help='Support debug', action='store_true')
parser.add_argument('-v', '--verbose', help='Be verbose', action='store_false')
parser.add_argument('-c', '--copytotarget', help='Copy to target in program files', action='store_true')
parser.add_argument('-b', '--build', help='build project', action='store_true')
parser.add_argument('-r', '--rebase', help='start rebase', action='store_true')
parser.add_argument('-u', '--undo', help='undo: start reset and revert changes', action='store_true')
parser.add_argument('-a', '--all', help='conduct all, i.e. rebase, build, copy', action='store_true')
args = parser.parse_args(None if sys.argv[1:] else ['-h'])

import subprocess, sys    
def buildProject():
  with subprocess.Popen(["powershell.exe", "-noprofile", "-command", ".\\build.ps1 64"],
    stdout = subprocess.PIPE, stderr = subprocess.PIPE, bufsize=1, universal_newlines=True
  ) as p:
    for line in p.stdout:
        print(line, end='') # process line here

def copyToTarget():
  with subprocess.Popen(
    [
      "powershell.exe", 
      "-noprofile", "-c",
      r"""
      exit (
        Start-Process -Verb RunAs -PassThru -Wait powershell.exe -Args "
          -noprofile -c Set-Location \`"$PWD\`"; 
          & Copy-Item -Force -Recurse"""
          + r""" -Path 'C:\Users\mweiss\source\shared\work\OneMore\OneMore\bin\x64\Debug\*' """
          + r""" -Destination 'C:\Program Files\River\OneMoreAddIn' -Verbose; exit `$LASTEXITCODE
        "
      ).ExitCode
      """
    ],
    stdout = subprocess.PIPE, stderr = subprocess.PIPE, bufsize=1, universal_newlines=True
  ) as p:
    for line in p.stdout:
        print(line, end='') # process line here

def rebase(reset_only = False):
  # for tracing, should be somehow merged with repo
  if args.verbose:
      from git.cmd import Git
      type(Git()).GIT_PYTHON_TRACE="full"
      g = Git('.')

  # repot
  from git import Repo
  repo = Repo('.')

  # List all branches for future use
  # for branch in repo.branches:
  #     print(branch)

  # handle dedicated branches
  # branches = ["improveMarkdown", "addPythonSupport", "fixWhenAll", "improveGerTranslation", "improvePlugInMenu", "misc", "makepublic"]
  branches = ["improveMarkdown", "addPythonSupport", "improveGerTranslation", "improvePlugInMenu", "misc", "makepublic"]
  # branches = ["improveGerTranslation"]
  for branch in branches:
      logger.info('---------------------------------------------')
      logger.info('Start handdling ' + branch )
      logger.info('---------------------------------------------')
      repo.git.checkout(branch)
      if reset_only:
          repo.git.reset("--hard", "origin/" +  branch)
          continue
      tag = datetime.datetime.today().strftime("%y-%m-%d") + "_" + branch 
      # create tag only if not already exist, assumption one per day is enough
      try:
          new_tag = repo.create_tag(tag, message='Automatic tag "{0}"'.format(tag)) 
          repo.git.push(tags=True)    
      except:
          logger.info('Tag exists allready, skip creation.')
          pass
      repo.git.rebase("main")

  # create new baseline
  logger.info('---------------------------------------------')
  logger.info('Start creation of ' + args.target )
  logger.info('---------------------------------------------')
  repo.git.checkout("main")
  if reset_only:
      repo.git.push('--delete', repo.remote().name, args.target)
      repo.delete_head(args.target) 
      return
  repo.git.checkout("-b",args.target)
  push_output = repo.git.push('--set-upstream', repo.remote().name, args.target)

  # merge branches
  for branch in branches:
      repo.git.merge(branch)
      repo.git.pull()
      repo.git.push()

  # final push    
  repo.git.push()

  if args.debug:
      # delete for debug purposes
      remote = repo.remote(name='origin')
      remote.push(refspec=(':' +  args.target))
    
if args.undo:
  rebase(reset_only=True)
  exit()

if args.rebase or args.all:
  rebase()

if args.build or args.all:
  buildProject()

if args.copytotarget or args.all:
  copyToTarget()


