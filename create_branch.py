# 
# script for automatic merging of multiple branches
#
import logging, datetime, argparse
logging.basicConfig(level=logging.INFO)

# parse parameter
parser = argparse.ArgumentParser("Parse options")
parser.add_argument('-t', '--target', help='Target branch', default="improveMD36")
parser.add_argument('-d', '--debug', help='Support debug', action='store_true')
parser.add_argument('-v', '--verbose', help='Be verbose', action='store_false')
args = parser.parse_args()

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
branches = ["improveMarkdown", "addPythonSupport", "fixWhenAll", "improveGerTranslation", "improvePlugInMenu", "misc", "makepublic"]
for branch in branches:
    repo.git.checkout(branch)
    tag = branch + "_working_" + datetime.datetime.today().strftime("%d%m%Y")
    # create tag only if not already exist, assumption one per day is enough
    try:
        new_tag = repo.create_tag(tag, message='Automatic tag "{0}"'.format(tag)) 
        repo.git.push(tags=True)    
    except:
        pass
    repo.git.rebase("main")

# create new baseline
repo.git.checkout("main")
repo.git.checkout("-b",args.target)
push_output = repo.git.push('--set-upstream', repo.remote().name, args.target)

# merge branches
for branch in branches:
    repo.git.merge(branch)

# final push    
repo.git.push()

if args.debug:
    # delete for debug purposes
    remote = repo.remote(name='origin')
    remote.push(refspec=(':' +  args.target))

