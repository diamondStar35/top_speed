/**
* Common library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __COMMON_DEFS_H__
#define __COMMON_DEFS_H__

#define SAFE_DELETE(p)       { if(p) { delete (p);     (p)=0; } }
#define SAFE_DELETE_ARRAY(p) { if(p) { delete[] (p);   (p)=0; } }
#define SAFE_RELEASE(p)      { if(p) { (p)->Release(); (p)=0; } }


#endif /* __COMMON_DEFS_H__ */
