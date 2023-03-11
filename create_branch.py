# 
# script for automatic merging of multiple branches
#
import logging, datetime, argparse, sys
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger('')

# parse parameter
parser = argparse.ArgumentParser( description = "Parse options", 
                                  epilog = "example of use: \n  python ../create_branch.py -t improveMDXXX -a \n ",
                                  formatter_class=argparse.RawDescriptionHelpFormatter)
parser.add_argument('-t', '--target', help='Target branch, use "XXX" to replace by next index', default="improveMDXXX")
parser.add_argument('-d', '--debug', help='Support debug', action='store_true')
parser.add_argument('-v', '--verbose', help='Be verbose', action='store_false')
parser.add_argument('-c', '--copytotarget', help='Copy to target in program files', action='store_true')
parser.add_argument('-b', '--build', help='build project', action='store_true')
parser.add_argument('-r', '--rebase', help='start rebase', action='store_true')
parser.add_argument('-u', '--undo', help='undo: start reset and revert changes', action='store_true')
parser.add_argument('-a', '--all', help='conduct all, i.e. rebase, build, copy', action='store_true')
parser.add_argument('-p', '--update', help='pull & push all branches of concern', action='store_true')
args = parser.parse_args(None if sys.argv[1:] else ['-h'])

import subprocess, re   
class GitBranchHelper():
  def __init__(self, args):
    super().__init__()
    self.args = args
    if self.args.verbose:
        from git.cmd import Git
        type(Git()).GIT_PYTHON_TRACE="full"
        self.g = Git('.')
    from git import Repo
    self.repo = Repo('.')
    if "XXX" in args.target:
        search_string = args.target.replace("XXX","")
        max_number = 0
        remote_refs = self.repo.remote().refs
        for refs in remote_refs:
            if search_string in refs.name:
                number = re.findall("(.*)improveMD(\d+)", refs.name, flags=re.MULTILINE)[0][1]    
                # print(search_string + " found here: " + refs.name + ". Number: " + number)
                max_number = max(max_number, int(number))
        print (" --> Maxnumber is " + str(max_number))
        self.args.target = args.target.replace("XXX", str(max_number+1))
    # List all branches for future use
    # for branch in repo.branches:
    #     print(branch)


  def buildProject():
    with subprocess.Popen(["powershell.exe", "-noprofile", "-command", ".\\build.ps1 64"],
      stdout = subprocess.PIPE, stderr = subprocess.PIPE, bufsize=1, universal_newlines=True, encoding="cp437"
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

  def rebase(self, reset_only = False, update_only = False):
    # handle dedicated branches
    # self.branches = ["improveMarkdown", "addPythonSupport", "fixWhenAll", "improveGerTranslation", "improvePlugInMenu", "misc", "makepublic"]
    # self.branches = ["improveMarkdown", "addPythonSupport", "improveGerTranslation", "improvePlugInMenu", "misc", "makepublic"]
    self.branches = ["improveMarkdown", "addPythonSupport", "improvePlugInMenu", "misc", "makepublic"]
    for branch in self.branches:
        logger.info('---------------------------------------------')
        logger.info('Start handdling ' + branch )
        logger.info('---------------------------------------------')
        
        self.repo.git.checkout(branch)
        if reset_only:
            self.repo.git.reset("--hard", "origin/" +  branch)
            continue
        # refresh branch    
        self.repo.git.pull()
        self.repo.git.push()
        if update_only:
            continue;
        tag = datetime.datetime.today().strftime("%y-%m-%d") + "_" + branch 
        # create tag only if not already exist, assumption one per day is enough
        try:
            new_tag = repo.create_tag(tag, message='Automatic tag "{0}"'.format(tag)) 
            self.repo.git.push(tags=True)    
        except:
            logger.info('Tag exists allready, skip creation.')
            pass
        self.repo.git.rebase("main")
        self.repo.git.push(force=True)

    if update_only:
        return
    # create new baseline
    logger.info('---------------------------------------------')
    logger.info('Start creation of ' + self.args.target )
    logger.info('---------------------------------------------')
    self.repo.git.checkout("main")
    self.repo.git.pull()
    if reset_only:
        self.repo.git.push('--delete', self.repo.remote().name, self.args.target)
        self.repo.delete_head(self.args.target, force=True) 
        return
    self.repo.git.checkout("-b",self.args.target)
    push_output = self.repo.git.push('--set-upstream', self.repo.remote().name, self.args.target)

    # merge branches
    for branch in self.branches:
        logger.info('---------------------------------------------')
        logger.info('Rebase ' + branch )
        logger.info('---------------------------------------------')
        self.repo.git.rebase(branch)
        self.repo.git.push(force=True)

    if self.args.debug:
        # delete for debug purposes
        remote = self.repo.remote(name='origin')
        remote.push(refspec=(':' +  self.args.target))
    
# start actual script
gitBranchHelper = GitBranchHelper(args)

if args.undo:
  gitBranchHelper.rebase(reset_only=True)
  exit()

if args.update:
  gitBranchHelper.rebase(update_only=True)
  exit()

if args.rebase or args.all:
  gitBranchHelper.rebase()

if args.build or args.all:
  GitBranchHelper.buildProject()

if args.copytotarget or args.all:
  GitBranchHelper.copyToTarget()


