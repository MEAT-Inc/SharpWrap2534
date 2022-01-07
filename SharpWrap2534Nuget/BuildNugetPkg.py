# Imports for module helpers
import os
import sys
import re
import time

# Store the args here and check if there's enough of them
args_array = sys.argv
if (len(args_array) < 4): 
    print ("Arguments are App Name, NuSpecFile, Version, Tag Value, [Changes]") 
    exit(0)

# Arg setup
# Arg 1 - Version Number
# Arg 2 - Tag Value
# Arg 3 - Change Notes

# Store nuspec file and version value
application_package_name = args_array[1]
template_nuspec_file = ".\\NupkgConfig\\_" + application_package_name + ".nuspec.base"
version_value = "<version>" + args_array[2] + "</version>"
version_tag_value = "<tags>" + args_array[3] + "</tags>"
if (len(args_array) == 3): 
    changelog_contents = ""
    description_contents = ""
else:
    changelog_contents = "<releaseNotes>" + args_array[4] + "\n</releaseNotes>"
    description_contents = args_array[4]

# Read contents of the nuspec file.
with open(template_nuspec_file, "r") as input_nuspec_file:
    nuspec_contents = input_nuspec_file.read()

    # Regex the nuspec file to build output.
    new_nuspec_contents = re.sub(r"<version>((\d+|\.|\>)+)<\/version>", version_value, nuspec_contents)
    new_nuspec_contents = re.sub(r"<tags>((\S)+)<\/tags>", version_tag_value, new_nuspec_contents)

    # Check if doing changelog.
    if (changelog_contents != ""):
        changelog_contents = re.sub(r"--\s+",  "\n- ", changelog_contents)
        new_nuspec_contents = re.sub(r"<releaseNotes>(([^<])+)<\/releaseNotes>", changelog_contents.strip(), new_nuspec_contents)

    # Check description update.
    if (description_contents != ""):
        description_contents = re.sub(r"--\s+", "\n- ", description_contents)
        new_nuspec_contents = re.sub(r"    <\/description>", description_contents.strip() + "\n</description>", new_nuspec_contents)
         
    # Close and delete file.
    input_nuspec_file.close()

# Write new values out and run the pack command.
output_spec_file = ".\\" + application_package_name + "." + args_array[2] + ".nuspec"
history_spec_file = ".\\NupkgConfig\\" + application_package_name + "." + args_array[2] + ".nuspec"
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
nuget_package_file = ".\\NupkgOutput\\" + application_package_name + "." + args_array[2] + '.nupkg'
if os.path.exists(nuget_package_file): os.remove(nuget_package_file) 

# Wait for settle.
time.sleep(1.5)
nuget_push_command = 'nuget push ' + nuget_package_file + " -Source \"github\" -ConfigFile .\\NupkgConfig\\_" + application_package_name + ".nuget.config -SkipDuplicate"
print ('Running nuget push now...')
print ('--> Push Command: ' + nuget_push_command) 
push_stream = os.popen(nuget_push_command)
print ('--> Pushed to nuget OK!')

# # Remove old files.
time.sleep(1.5)
if os.path.exists(output_spec_file): os.remove(output_spec_file) 

# Split output
print ('-------------------------------------------------------------')
print ('')