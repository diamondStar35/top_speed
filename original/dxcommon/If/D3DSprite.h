/**
* DXCommon library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __DXCOMMON_D3DSPRITE_H__
#define __DXCOMMON_D3DSPRITE_H__

#include <DxCommon/If/Internal.h>
#include <DxCommon/If/Game.h>


namespace DirectX
{

class D3DSprite;
class D3DSpriteManager;


class D3DSpriteManager
{
public:
    _dxcommon_          D3DSpriteManager(LPDIRECT3DDEVICE8 d3dDevice, D3DFORMAT textureFormat, 
                                         Char* textureFile, UInt transparantColor);
    _dxcommon_ virtual ~D3DSpriteManager( );

public:
    _dxcommon_ void     render(D3DSprite* sprite, Game::Control3D* control3D);

private:
    LPDIRECT3DDEVICE8       m_d3dDevice;
    LPDIRECT3DTEXTURE8      m_texture;
    LPDIRECT3DVERTEXBUFFER8 m_vertexBuffer;
    UInt                    m_textureWidth;
    UInt                    m_textureHeight; 
};



class D3DSprite
{
public:
    friend class D3DSpriteManager;
public:
    _dxcommon_ D3DSprite(Vector3 position, Vector3 velocity);
    _dxcommon_ virtual ~D3DSprite( );

public:
    _dxcommon_ Vector3 position( ) const                    { return m_position;       }
    _dxcommon_ Vector3 velocity( ) const                    { return m_velocity;       }
    _dxcommon_ Float   size( ) const                        { return m_size;           }
    _dxcommon_ UInt    textureOffsetX( ) const              { return m_textureOffsetX; }
    _dxcommon_ UInt    textureOffsetY( ) const              { return m_textureOffsetY; }

    _dxcommon_ void    position(Vector3 pos)                { m_position = pos;        }
    _dxcommon_ void    velocity(Vector3 vel)                { m_velocity = vel;        }
    _dxcommon_ void    size(Float size)                     { m_size     = size;       }
    _dxcommon_ void    textureOffsetX(UInt x)               { m_textureOffsetX = x;    }
    _dxcommon_ void    textureOffsetY(UInt y)               { m_textureOffsetY = y;    }
    
protected:
    D3DSprite*     m_next;          // Link to next object
    D3DSprite*     m_prev;          // Link to previous object
    
    Boolean        m_visible;          // Whether the object is visible
    Vector3        m_position;              // Position
    Vector3        m_velocity;              // Velocity
    Float          m_size;

    UInt  m_nFramesPerLine;   // How anim frames are packed in bitmap
    UInt  m_currentFrame;             // Current animation frame
    UInt  m_maxFrame;         // Max animation frame value
    Float m_delay;             // Frame/second
    
    UInt  m_color;

    UInt  m_textureOffsetX; // Pixel offsets into the game texture
    UInt  m_textureOffsetY;
    UInt  m_textureWidth;   // Width and height in pixels
    UInt  m_textureHeight; 
};




} // namespace DirectX


#endif /* __DXCOMMON_D3DFONT_H__ */
