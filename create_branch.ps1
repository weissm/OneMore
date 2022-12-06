git checkout -b testbranch mymain;
git push --set-upstream origin testbranch;
git merge improveMarkdown;
git push;
git merge addPythonSupport;
git push;
git merge fixWhenAll;
git push;
git merge improveGerTranslation;
git push;
git merge improvePlugInMenu;
git push;
git merge misc;
git push;
git merge makepublic;
git push

