# Imports for module helpers
import os
import sys
import re
import time

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
template_nuspec_file = ".\\NupkgConfig\\_SharpWrap2534.nuspec.base"
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
output_spec_file = ".\\SharpWrap2534_" + args_array[1] + '.nuspec'
history_spec_file = '.\\NupkgConfig\\SharpWrap2534_' + args_array[1] + '.nuspec'
if os.path.exists(output_spec_file): os.remove(output_spec_file) 
if os.path.exists(history_spec_file): os.remove(history_spec_file) 

# Write new ones out here.
with open (output_spec_file, "w") as nuspec_file:
    nuspec_file.write(new_nuspec_contents)
    nuspec_file.close()
with open (history_spec_file, "w") as history_nuspec_file:
    history_nuspec_file.write(new_nuspec_contents)
    history_nuspec_file.close()

# Run the pack command now.
print ('')
nuget_pack_command = 'nuget pack ' + output_spec_file + ' -OutputDirectory .\\NupkgOutput'
print ('Running nuget pack now...')
print ('--> Pack Command: ' + nuget_pack_command)
pack_stream = os.popen(nuget_pack_command)
print ('--> Packed Package OK!')

# Split output
print ('-------------------------------------------------------------')

# Push the package into the repo
nuget_package_file = '.\\NupkgOutput\\SharpWrap2534.' + args_array[1] + '.nupkg'
if os.path.exists(nuget_package_file): os.remove(nuget_package_file) 

# Wait for settle.
time.sleep(1.5)
nuget_push_command = 'nuget push ' + nuget_package_file + ' -Source \"github\" -ConfigFile .\\NupkgConfig\\_SharpWrap2534.nuget.config -SkipDuplicate'
print ('Running nuget push now...')
print ('--> Push Command: ' + nuget_push_command) 
push_stream = os.popen(nuget_push_command)
print ('--> Pushed to nuget OK!')

# Remove old files.
time.sleep(1.5)
if os.path.exists(output_spec_file): os.remove(output_spec_file) 

# Split output
print ('-------------------------------------------------------------')
print ('')