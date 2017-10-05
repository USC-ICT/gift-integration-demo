charNames = scene.getCharacterNames()
chars = ""
for i in range(0, len(charNames)):
	chars += charNames[i] + " "
	
scene.command("sbm send  vht_characters " + chars )  
	