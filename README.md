# ImageDifferentiator

**todo:**
- add ImageDataTypeList class to the app

- also finish thread functionality (https://stackoverflow.com/questions/811224/how-to-create-a-thread ?? https://stackoverflow.com/questions/50070179/writing-to-a-text-file-i-o-error ??)
- dont write findings in file just yet, coz multithreading is gonna be overwhelmed by it, make list where findings gonna be
- different image ratio squishing to check badly cropped images
- far future optimization: check what file is locked atm and if cant open, then move on, and come back later (also handle it smartly, so they dont come back at the same time) (this gonna requre to add a new list inthe imgdatatype, so we have a list what we have checked sofar, tedious, im lazy, maybe far future will be implemented)
