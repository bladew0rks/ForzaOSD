# tnx to Xenta and Krunky for kindly let me using writeCFG and loadCFG from k-shifter

import ac
import configparser , os.path , traceback

config = configparser.SafeConfigParser()

def writeCFG(dirDest, fileDest, sectionDest, varName, varDest):
    
  try:
      
    if not os.path.exists(dirDest):
      os.makedirs(dirDest)
    
    if os.path.exists(dirDest + fileDest):
      config.read(dirDest + fileDest)
    
    if not config.has_section(sectionDest):
      config.add_section(sectionDest)
        
    config.set(sectionDest, varName, repr(varDest))
    
    with open(dirDest + fileDest, 'wt') as configfile:
      config.write(configfile)
  
  except Exception:
    ac.console("PDash : Error in writeCFG(): %s" % traceback.format_exc())
    ac.log("PDash : Error in writeCFG(): %s" % traceback.format_exc())
    ac.log("writeCFG called with dirDest="+dirDest+",fileDest="+fileDest+",sectionDest="+sectionDest+",varName="+varName+",varDest="+repr(varDest))   
    
def loadCFG(dirDest, fileDest, sectionDest, varName, varDest):

  try:
      
    if os.path.exists(dirDest + fileDest):
      config.read(dirDest + fileDest)
      if config.has_option(sectionDest, varName):
        varDest = config.get(sectionDest, varName)
      else:
        varDest = False  
  
  except Exception:
    ac.console("PDash : Error in loadCFG(): %s" % traceback.format_exc())
    ac.log("PDash : Error in loadCFG(): %s" % traceback.format_exc())
    ac.log("writeCFG called with dirDest="+dirDest+",fileDest="+fileDest+",sectionDest="+sectionDest+",varName="+varName+",varDest="+repr(varDest))   
  
  return varDest