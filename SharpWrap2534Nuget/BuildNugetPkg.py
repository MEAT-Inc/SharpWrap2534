# Imports for module helpers
import os
import sys
import re

# Store the args here and check if there's enough of them
args_array = sys.argv
if (len(args_array) < 4): 
    print ("Arguments are NuSpecFile, Version, Tag Value, [Changes]") 
    exit(0)

# Arg setup
# Arg 0 - Path to nuspec
# Arg 1 - Version Number
# Arg 2 - Tag Value
# Arg 3 - Change Notes

# Store nuspec file and version value
nuspec_file_name = args_array[1]
version_value = "<version>" + args_array[2] + "</version>"
version_tag_value = "<tags>" + args_array[3] + "</tags>"
if (len(args_array) == 4): changelog_contents = ""
else: changelog_contents = "<releaseNotes>" + args_array[4] + "</releaseNotes>"

# Read contents of the nuspec file.
with open(nuspec_file_name, "r+") as nuspec_file:
    nuspec_contents = nuspec_file.read()

    # Regex the nuspec file to build output.
    new_nuspec_contents = re.sub("<version>((\d+|\.|\>)+)<\/version>", version_value, nuspec_contents)
    new_nuspec_contents = re.sub("<tags>((\S)+)<\/tags>", version_tag_value, new_nuspec_contents)

    # Check if doing changelog.
    if (changelog_contents != ""):
        new_nuspec_contents = re.sub("<releaseNotes>(([^<])+)<\/releaseNotes>", changelog_contents, new_nuspec_contents)

    # Close and delete file.
    nuspec_file.close()


# Write new values out and run the pack command.
os.remove(nuspec_file_name)
with open (nuspec_file_name, "w") as nuspec_file:
    nuspec_file.write(new_nuspec_contents)
    nuspec_file.close()

# Run the pack command now.
nuget_command = 'nuget pack ' + nuspec_file_name + ' -OutputDirectory NupkgFiles'
print ('Running nuget pack command: ' + nuget_command)
os.popen(nuget_command)