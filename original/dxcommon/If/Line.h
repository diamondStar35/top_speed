/**
* DXCommon library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __DXCOMMON_LINE_H__
#define __DXCOMMON_LINE_H__

#include <DxCommon/If/Internal.h>

namespace DirectX
{


struct Vertex
{
	Float    x, y, z;
	UInt     color;
};



class Line
{
public:
    _dxcommon_ Line(LPDIRECT3DDEVICE8 d3dDevice, Vector3 from, Vector3 to, UInt color);
    _dxcommon_ ~Line( );

public:
    _dxcommon_ void render(LPDIRECT3DDEVICE8 d3dDevice);

private:
    LPDIRECT3DVERTEXBUFFER8 m_vertexBuffer;
};


}; // namespace DirectX








#endif /* __DXCOMMON_LINE_H__ */
