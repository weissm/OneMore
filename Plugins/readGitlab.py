#
import traceback
import datetime

import argparse
parser = argparse.ArgumentParser("Parse options")
parser.add_argument('-t', '--token', help='Specify gitlab token')
parser.add_argument('-u', '--url', help='Specify gitlab url')
parser.add_argument('-w', '--workFile', help='Specify XML work file')
parser.add_argument('-i', '--inputLink', help='Specify input Link')
parser.add_argument('-f', '--debugFile', help='Specify debugFile')
parser.add_argument('-l', '--onenoteLink', help='Specify onenoteLink')
args = parser.parse_args()

from xml.dom import minidom
xmldoc = minidom.parse(args.workFile)

root = xmldoc.documentElement
a = root.childNodes.item(1)
ns = a.namespaceURI

### Gitlab
import gitlab
from urllib.parse import urlparse
import os

# private token or personal token authentication (self-hosted GitLab instance)
gl = gitlab.Gitlab(url=args.url, private_token=args.token)

targetURI = urlparse(args.inputLink)
targetProject = targetURI.path.split("/-/issues",1)[0][1:]
targetID = int(targetURI.path.rsplit('/', 1)[1])

project = gl.projects.get(targetProject)
issue = issue = project.issues.get(targetID)
print(issue.description)

for line in issue.description.splitlines():
    element2 = xmldoc.createElementNS(ns, "one:T")
    element2.appendChild(xmldoc.createCDATASection(line))
    element = xmldoc.createElementNS(ns, "one:OE")
    element.appendChild(element2)
    cd = xmldoc.getElementsByTagNameNS(ns, "OE")
    elements = len(cd) - 1
    cd[elements].parentNode.insertBefore(element, cd[elements])

# for debug only
with open(args.debugFile, "w") as xml_file:
    xmldoc.writexml(xml_file)

with open(args.workFile, "w") as xml_file:
    xmldoc.writexml(xml_file)

