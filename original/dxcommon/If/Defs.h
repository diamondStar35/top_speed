/**
* DXCommon library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __DXCOMMON_DEFS_H__
#define __DXCOMMON_DEFS_H__

#include "d3d8types.h"
#include <DxCommon/If/Internal.h>

#define WAVEFILE_READ   1
#define WAVEFILE_WRITE  2

enum
{
    dxSuccess          =   0,
    dxFailed           =  -1,
    dxJoystickNotFound =  -2,

    dxCallOnlyOnce     =   1
};


namespace DirectX
{

const Float Pi = 3.141592654f;

namespace Color
{
    const Int Green     = D3DCOLOR_ARGB(0xff, 0x00, 0xff, 0x00);

    enum 
    {
        white       = 0xffffffff, //D3DCOLOR_ARGB(0xff, 0xff, 0xff, 0xff)
        black       = 0xff000000, //D3DCOLOR_ARGB(0xff, 0x00, 0x00, 0x00)
        grey        = 0xff7f7f7f, //D3DCOLOR_ARGB(0xff, 0x7f, 0x7f, 0x7f)
        grey20      = 0xffcccccc, //D3DCOLOR_ARGB(0xff, 0xcc, 0xcc, 0xcc)
        grey70      = 0xff555555, //D3DCOLOR_ARGB(0xff, 0x33, 0x33, 0x33)
        grey80      = 0xff333333, //D3DCOLOR_ARGB(0xff, 0x33, 0x33, 0x33)
        red         = 0xffff0000, //D3DCOLOR_ARGB(0xff, 0xff, 0x00, 0x00)
        green       = 0xff00ff00, //D3DCOLOR_ARGB(0xff, 0x00, 0xff, 0x00)
        blue        = 0xff0000ff, //D3DCOLOR_ARGB(0xff, 0x00, 0x00, 0xff)
        yellow      = 0xffffff00, //D3DCOLOR_ARGB(0xff, 0xff, 0xff, 0x00)
        brown       = 0xff663333, //D3DCOLOR_ARGB(0xff, 0x66, 0x33, 0x33)
        pink        = 0xffff99cc, //D3DCOLOR_ARGB(0xff, 0xff, 0x99, 0xcc)
        purple      = 0xff9900cc, //D3DCOLOR_ARGB(0xff, 0x99, 0x00, 0xcc)
        orange      = 0xffff6600, //D3DCOLOR_ARGB(0xff, 0xff, 0x66, 0x00)
        grassgreen  = 0xff009933, //D3DCOLOR_ARGB(0xff, 0x00, 0x99, 0x33)
        lightyellow = 0xffffff7f  //D3DCOLOR_ARGB(0xff, 0xff, 0xff, 0x7f)
    };
} // namespace Color

} // namespace DirectX




#endif /* __DXCOMMON_DEFS_H__ */
