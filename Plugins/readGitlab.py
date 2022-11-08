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
print(xmlDoc)
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
print(issue.description)
# 
# add markdown into onenote xml
#
import xml.etree.ElementTree as ET
etXml = ET.parse(args.workFile)
etRoot = etXml.getroot()
# read standard vars
ns = etRoot.tag.split("}")[0]+"}"
creation_time = etRoot.find(f"Title")


elementTitle = xmlDoc.getElementsByTagNameNS(ns, "Title")
test2 = elementTitle.getElementsByTagNameNS(ns, "T")
test = elementTitle[0].childNodes[0].firstChild.firstChild.nodeValue
print (test)
resultado = []
for j in range(len(elementTitle)):
    resultado.append(xmlDoc.getElementsByTagNameNS(ns, 'Title')[j].firstChild.data)
    print(resultado[j])
elementT = elementTitle.getElementsByTagNameNS(ns, "T")
elementT.firstChild.nodeValue = issue.title

for line in issue.description.splitlines():
    elementT = xmlDoc.createElementNS(ns, "one:T")
    elementT.appendChild(xmlDoc.createCDATASection(line))
    elementOE = xmlDoc.createElementNS(ns, "one:OE")
    elementOE.appendChild(elementT)
    elementName = xmlDoc.getElementsByTagNameNS(ns, "OE")
    elementLen = len(elementName) - 1
    elementName[elementLen].parentNode.insertBefore(elementOE, elementName[elementLen])

escapeID = "[CLD-"
markdown = escapeID + "Title" + "] " + issue.title + "\n";
markdown += issue.description
# process doc
# see here https://github.com/pythonnet/pythonnet/wiki/How-to-call-a-dynamic-library
import sys, clr
sys.path.append(r"c:\Program Files\River\OneMoreAddIn")
clr.AddReference('River.OneMoreAddIn')
from River.OneMoreAddIn.Commands import ImportWebCommand
ImportWebCommand.ImportAsMarkdown(args.inputLink, markdown)

#
# write results
#
with open(args.workFile, "w") as xml_file:
    xmlDoc.writexml(xml_file)

