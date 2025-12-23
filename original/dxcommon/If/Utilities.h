/**
* DXCommon library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __DXCOMMON_UTILITIES_H__
#define __DXCOMMON_UTILITIES_H__

#include <DxCommon/If/Internal.h>

namespace DirectX
{

_dxcommon_ Int   findFile(Char* path, Char* filename);

_dxcommon_ Int   createTexture(LPDIRECT3DDEVICE8 d3dDevice, D3DFORMAT textureFormat, 
                               LPDIRECT3DTEXTURE8* texture, Char* textureFile);
_dxcommon_ Int   setTransparantColor(LPDIRECT3DTEXTURE8 texture, UInt color);

_dxcommon_ void  convertStringToWide(WCHAR* dest, const Char* source, int stringLen = -1);
_dxcommon_ void  convertWideToString(Char* dest, const WCHAR* source, int stringLen = -1);

}; // namespace DirectX


#endif /* __DXCOMMON_UTILITIES_H__ */
