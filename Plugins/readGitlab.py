#
# define program arguments
#
import argparse
parser = argparse.ArgumentParser("Parse options")
parser.add_argument('-t', '--token', help='Specify gitlab token')
parser.add_argument('-u', '--url', help='Specify gitlab url')
parser.add_argument('-w', '--workFile', help='Specify XML work file')
parser.add_argument('-i', '--inputLink', help='Specify input Link')
parser.add_argument('-f', '--debugFile', help='Specify debugFile')
parser.add_argument('-l', '--onenoteLink', help='Specify onenoteLink')
args = parser.parse_args()
#
# read xml
#
from xml.dom import minidom
xmlDoc = minidom.parse(args.workFile)
# extract parameters
root = xmlDoc.documentElement
a = root.childNodes.item(1)
ns = a.namespaceURI
#
# Gitlab
#
import gitlab
from urllib.parse import urlparse
# private token or personal token authentication (self-hosted GitLab instance)
gl = gitlab.Gitlab(url=args.url, private_token=args.token)
# extract file infos
targetURI = urlparse(args.inputLink)
targetProject = targetURI.path.split("/-/issues",1)[0][1:]
targetID = int(targetURI.path.rsplit('/', 1)[1])
# get projects
project = gl.projects.get(targetProject)
issue = project.issues.get(targetID)
# 
# add markdown into onenote xml
#
if False:
    elementTitle = xmlDoc.getElementsByTagNameNS(ns, "Title")[0].getElementsByTagNameNS(ns, "T")[0]
    # Set its text content
    if elementTitle.firstChild:
        # Assumes that the first child is in fact a text node
        elementTitle.firstChild.nodeValue = issue.title
    else:
        # If the element is empty, add a child node
        elementTitle.appendChild(xmlDoc.createTextNode(issue.title))

    for line in issue.description.splitlines():
        elementT = xmlDoc.createElementNS(ns, "one:T")
        elementT.appendChild(xmlDoc.createCDATASection(line))
        elementOE = xmlDoc.createElementNS(ns, "one:OE")
        elementOE.appendChild(elementT)
        elementName = xmlDoc.getElementsByTagNameNS(ns, "OE")
        elementLen = len(elementName) - 1
        elementName[elementLen].parentNode.insertBefore(elementOE, elementName[elementLen])
    #
    # write results
    #
    with open(args.workFile, "w") as xml_file:
        xmlDoc.writexml(xml_file)

else:
    # process doc
    # see here https://github.com/pythonnet/pythonnet/wiki/How-to-call-a-dynamic-library
    import sys, clr
    sys.path.append(r"c:\Program Files\River\OneMoreAddIn")
    clr.AddReference('River.OneMoreAddIn')
    from River.OneMoreAddIn.Commands import ImportWebCommand
    ImportWebCommand.ImportAsMarkdown(args.inputLink, issue.description, issue.title)
