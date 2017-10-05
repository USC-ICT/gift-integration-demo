
pushd NVBG\bin\Release
start NVBG.exe -write_to_file false -data_folder_path ../../../data/nvbg-toolkit -parsetree_cachefile_path ../../../data/cache/brad_rachel_parse.txt -create_character Brad Brad.ini -create_character Rachel Rachel.ini -storypoint toolkitsession
popd
