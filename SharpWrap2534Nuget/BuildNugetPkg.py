# Imports for module helpers
import os
import sys
import re

# Store the args here and check if there's enough of them
args_array = sys.argv
if (len(args_array) < 3): 
    print ("Arguments are NuSpecFile, Version, Tag Value, [Changes]") 
    exit(0)

# Arg setup
# Arg 1 - Version Number
# Arg 2 - Tag Value
# Arg 3 - Change Notes

# Store nuspec file and version value
template_nuspec_file = "NupkgConfig\\_SharpWrap2534.nuspec.base"
version_value = "<version>" + args_array[1] + "</version>"
version_tag_value = "<tags>" + args_array[2] + "</tags>"
if (len(args_array) == 3): changelog_contents = ""
else: changelog_contents = "<releaseNotes>" + args_array[3] + "</releaseNotes>"

# Read contents of the nuspec file.
with open(template_nuspec_file, "r") as input_nuspec_file:
    nuspec_contents = input_nuspec_file.read()

    # Regex the nuspec file to build output.
    new_nuspec_contents = re.sub("<version>((\d+|\.|\>)+)<\/version>", version_value, nuspec_contents)
    new_nuspec_contents = re.sub("<tags>((\S)+)<\/tags>", version_tag_value, new_nuspec_contents)

    # Check if doing changelog.
    if (changelog_contents != ""):
        new_nuspec_contents = re.sub("<releaseNotes>(([^<])+)<\/releaseNotes>", changelog_contents, new_nuspec_contents)

    # Close and delete file.
    input_nuspec_file.close()

# Write new values out and run the pack command.
output_spec_file = "SharpWrap2534_" + version_value + '.nuspec'
with open (output_spec_file, "w") as nuspec_file:
    nuspec_file.write(new_nuspec_contents)
    nuspec_file.close()

# Run the pack command now.
nuget_pack_command = 'nuget pack ' + output_spec_file + ' -OutputDirectory .\\NupkgOutput'
print ('Running nuget pack command: ' + nuget_pack_command)
os.popen(nuget_pack_command)

# Split output
print ('-------------------------------------------------------------')

# Push the package into the repo
nuget_package_file = '.\\NupkgOutput\\SharpWrap2534.' + version_value + '.nupkg'
nuget_push_command = 'nuget push ' + nuget_package_file + '-Source \"github\" -ConfigFile NupkgConfig\\_SharpWrap2534.nuget.config'
print ('Running nuget push command: ' + nuget_push_command) 
os.popen(nuget_push_command)

# Split output
print ('-------------------------------------------------------------')