rm docs/index.md
for file in $(ls ../readme-parts/*.md | sort); do
  echo "Adding $file"
  grep -v -e '^<details>' -e '^</details>' -e '^<summary>' -e '^</summary>' -e '^# ' "$file" >> docs/index.md
done

mkdocs build
rm site/sitemap.xml
rm site/sitemap.xml.gz
rm -rf ../../server/formcms.course/wwwroot/doc
cp -r site ../../server/formcms.course/wwwroot/doc
sed -i '' 's|<a class="navbar-brand" href=".">|<a class="navbar-brand" href="/">|' ../../server/formcms.course/wwwroot/doc/index.html
