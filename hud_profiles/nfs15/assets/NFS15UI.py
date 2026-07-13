# PDash by Fabio Montarsolo (PandaR1) v1.5.4

# thanks to
# Rombik - Shared Memory siminfo code
# Xenta and Krunky for kindly let me using writeCFG and loadCFG from k-shifter
# https://www.racedepartment.com/members/stormix43.412027/

import g
import ac
from initall import *

def acMain(self):

  ac.log('PDash - application loaded')      
  init_all()  
     
def acUpdate(deltaT):  
  
  for d in g.dashes:
    d.refresh(deltaT)   

