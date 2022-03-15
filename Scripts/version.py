import yaclog
import yaclog.version
import git as gp
import os
import xml.dom.minidom as minidom
import json


def run():
    repo = gp.Repo(os.curdir)
    cl = yaclog.Changelog('CHANGELOG.md')
    version = str(cl.current_version(released=True).version)
    release = False

    for tag in repo.tags:
        if tag.commit == repo.head.commit:
            release = True
            build = 100000
            break

    if not release:
        build = int.from_bytes(repo.head.commit.binsha[0:2], byteorder='big')
        version = yaclog.version.increment_version(version, 2)

    print(f'Setting up version {version} build {build}')

    version_path = 'GameData/ConformalDecals/Versioning/ConformalDecals.version'
    with open(version_path, 'r+') as version_file:
        print('Updating version file')
        segments = version.split('.')
        # print(version_file.read())
        decoded = json.load(version_file)
        decoded['VERSION']['MAJOR'] = int(segments[0])
        decoded['VERSION']['MINOR'] = int(segments[1])
        decoded['VERSION']['PATCH'] = int(segments[2])
        decoded['VERSION']['BUILD'] = build

        version_file.seek(0)
        json.dump(decoded, version_file, indent=4)
        version_file.truncate()

    project_path = 'Source/ConformalDecals/ConformalDecals.csproj'
    with open(project_path, 'r+') as project_file:
        print('Updating csproj file')
        segments = version.split('.')
        decoded = minidom.parse(project_file)
        version_node = decoded.getElementsByTagName('AssemblyVersion')[0]
        if release:
            version_node.firstChild.nodeValue = f'{version}'
        else:
            version_node.firstChild.nodeValue = f'{version}.{build}'
        # version_node.value = f'{version}.{build}'
        project_file.seek(0)
        decoded.writexml(project_file)
        project_file.truncate()

    print('Done!')


if __name__ == '__main__':
    run()
