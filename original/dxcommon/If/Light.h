/**
* DXCommon library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __DXCOMMON_LIGHT_H__
#define __DXCOMMON_LIGHT_H__

#include <DxCommon/If/Common.h>


#include <d3d8.h>
#include <d3dx8.h>



namespace DirectX
{

class Light
{
public:
    enum Type
    {
        directional = D3DLIGHT_DIRECTIONAL,
        point       = D3DLIGHT_POINT,
        spot        = D3DLIGHT_SPOT
    };

public:
    _dxcommon_ Light(Type, Vector3 position, Vector3 direction);
    _dxcommon_ Light(Type, Vector3 position, Vector3 direction, Int color);
    _dxcommon_ virtual ~Light( );

public:
    _dxcommon_ void enable(LPDIRECT3DDEVICE8 d3dDevice, UInt number);
    _dxcommon_ void disable(LPDIRECT3DDEVICE8 d3dDevice, UInt number);

public:
    D3DLIGHT8    m_light;

};


} // namespace DirectX



#endif /* __DXCOMMON_LIGHT_H__ */
