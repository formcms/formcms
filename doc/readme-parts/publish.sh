#!/bin/bash

set -e

echo "Merging readme.. "
for file in $(ls ./*.md | sort); do
  echo "Adding $file"
  cat "$file" >> readme.md
done
cp readme.md ../../../FormCmsAdminApp/README.md
cp readme.md ../../../FormCmsAdminApp/libs/FormCmsAdminSdk/README.md
mv readme.md ../../readme.md

echo "Making MkDoc readme.. "
for file in $(ls ./*.md | sort); do
  echo "Adding $file"
  grep -v -e '^<details>' -e '^</details>' -e '^<summary>' -e '^</summary>' -e '^# ' "$file" >> index.md
done
mv index.md ../mkdoc/docs

echo "switch to mk doc"
pushd ../mkdoc

echo "build mk doc"
mkdocs build 
rm site/sitemap.xml
rm site/sitemap.xml.gz
rm -rf ../../server/formcms.course/wwwroot/doc
cp -r site ../../server/formcms.course/wwwroot/doc
sed -i '' 's|<a class="navbar-brand" href=".">|<a class="navbar-brand" href="/">|' ../../server/formcms.course/wwwroot/doc/index.html
popd
echo "succeed!!!!!!!!!!!!"


