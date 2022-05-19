# Imports for module helpers
import os
import sys
import re
import time

# Store the args here and check if there's enough of them
args_array = sys.argv
if (len(args_array) < 5): 
    print ("Arguments are App Name, Version, Tag Value, Location (Local,Remote,Both), [Changes. Use '-- (Note/Entry)' to split lines of input into bullet points]") 
    exit(0)

# Arg setup
# Arg 1 - App Name
# Arg 2 - Version Number
# Arg 3 - Tag Value
# Arg 2 - Location Output
# Arg 4 - Change Notes

# Store nuspec file and version value
application_package_name = args_array[1]
template_nuspec_file = ".\\NupkgConfig\\_" + application_package_name + ".nuspec.base"
version_value = "<version>" + args_array[2] + "</version>"
version_tag_value = "<tags>" + args_array[3] + "</tags>"
nuget_push_location = args_array[4].upper()
if (len(args_array) == 4): 
    changelog_contents = ""
    description_contents = ""
else:
    changelog_contents = "<releaseNotes>" + args_array[5] + "\n</releaseNotes>"
    description_contents = args_array[5]

# Make sure our nuget destination type is valid
if (nuget_push_location != "LOCAL" and nuget_push_location != "REMOTE" and nuget_push_location != "BOTH"):
    print ('Error! Must provide one of the following locations for our output!')
    print ('--> Local   | Saves packages to local machine only')
    print ('--> Remote  | Saves packages to external server on nuget only')
    print ('--> Both    | Saves packages to both the local machine and nuget server')
    exit(0)

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
print ('\n-------------------------------------------------------------')
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

# Dump this into our local package server
time.sleep(1.5)
if (nuget_push_location == "LOCAL" or nuget_push_location == "BOTH"):
    # Setup package location values 
    local_server_directory = 'G:\\MEAT-Inc_NugetPackages'
    local_package_directory = local_server_directory + '\\' + application_package_name

    # Print information, build output packages. Check each dir value here.
    print ('Adding to local Nuget Server Location now...')
    print ('--> Server location on local machine: ' + local_server_directory)

    # Check for base server location
    if (os.path.isdir(local_server_directory) == False): 
        print ('--> Building base server location for Nuget packages...')
        print ('--> Directory being built: ' + local_server_directory)
        os.mkdir(local_server_directory)
    
    # Check for local package location
    if (os.path.isdir(local_package_directory) == False):
        print ('--> Building directory for nuget package ' + application_package_name + ' now...')  
        print ('--> Directory being built: ' + local_server_directory)

        # Build our init routine and run the output command.
        print ('--> Building new Nuget package instance for application ' + application_package_name + '...')
        init_nuget_command = 'nuget init ' + os.getcwd() + ' ' + local_server_directory
        print ('--> Init Command: ' + init_nuget_command)
        init_stream = os.popen(init_nuget_command)
        print ('--> Init routine passed!')
        print ('-------------------------------------------------------------')

    # Add the package locally now
    nuget_local_add = 'nuget add ' + nuget_package_file + ' -source ' + local_server_directory
    print ('--> Add Command: ' + nuget_local_add)
    local_stream = os.popen(nuget_local_add)
    print ('--> Added to local Nuget feed OK!')

# Wait for settle. Pushing to nuget only if changes are given
if (nuget_push_location == "REMOTE" or nuget_push_location == "BOTH"):
    print ('-------------------------------------------------------------')
    nuget_push_command = 'nuget push ' + nuget_package_file + " -Source \"github\" -ConfigFile .\\NupkgConfig\\_" + application_package_name + ".nuget.config -SkipDuplicate"
    print ('Running nuget push now...')
    print ('--> Push Command: ' + nuget_push_command) 
    push_stream = os.popen(nuget_push_command)
    print ('--> Pushed to nuget OK!')

# Remove old files.
time.sleep(1.5)
if os.path.exists(output_spec_file): os.remove(output_spec_file) 

# Split output
print ('-------------------------------------------------------------')