/**
* DXCommon library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#include <DxCommon/If/D3DSprite.h>
#include <DxCommon/If/Utilities.h>

#include <D3dx8math.h>

namespace DirectX
{

struct SPRITEVERTEX
{
    D3DXVECTOR3 p;
    DWORD       color;
    FLOAT       tu, tv;
};

#define D3DFVF_SPRITEVERTEX (D3DFVF_XYZ|D3DFVF_DIFFUSE|D3DFVF_TEX1)


D3DSpriteManager::D3DSpriteManager(LPDIRECT3DDEVICE8 d3dDevice, D3DFORMAT textureFormat, 
                                   Char* textureFile, UInt transparantColor) :
    m_d3dDevice(d3dDevice),
    m_texture(0),
    m_vertexBuffer(0)
{
    if (createTexture(d3dDevice, textureFormat, &m_texture, textureFile) != dxSuccess)
    {
        DXCOMMON("(!) D3DSpriteManager : failed to create texture from file %s", textureFile);
        return;
    }

    if (setTransparantColor(m_texture, transparantColor) != dxSuccess)
    {
        DXCOMMON("(!) D3DSpriteManager : failed to set the transparant color for file %s", textureFile);
        return;
    }

    // Create the vertexbuffer for rendering the sprites
    if (FAILED(d3dDevice->CreateVertexBuffer(4*sizeof(SPRITEVERTEX),
                                             D3DUSAGE_WRITEONLY, D3DFVF_SPRITEVERTEX,
                                             D3DPOOL_MANAGED, &m_vertexBuffer)))
    {
        SAFE_DELETE(m_texture);
        DXCOMMON("(!) D3DSpriteManager : failed to create vertexbuffer for sprites.");
        return;
    }
    
    // Find out what the width and height is of the texture
    D3DSURFACE_DESC surface;
    m_texture->GetLevelDesc(0, &surface);
    m_textureWidth  = surface.Width;
    m_textureHeight = surface.Height;
}



D3DSpriteManager::~D3DSpriteManager( )
{
    SAFE_DELETE(m_texture);
    SAFE_DELETE(m_vertexBuffer);
}


void D3DSpriteManager::render(D3DSprite* sprite, Game::Control3D* control3D)
{
    Float fX = sprite->m_position.x;
    Float fY = sprite->m_position.y;
    Float fZ = sprite->m_position.z;
    
    Float x1 = -(sprite->m_size);
    Float x2 =  sprite->m_size;
    Float y1 = -(sprite->m_size);
    Float y2 =  sprite->m_size;

    Float u1 = (Float)(sprite->m_textureOffsetX + 
                       sprite->m_textureWidth *(((UInt)sprite->m_currentFrame)%sprite->m_nFramesPerLine));
    Float v1 = (Float)(sprite->m_textureOffsetY + 
                       sprite->m_textureHeight*(((UInt)sprite->m_currentFrame)/sprite->m_nFramesPerLine));

    Float tu1 = u1 / (256.0f-1.0f);
    Float tv1 = v1 / (256.0f-1.0f);
    Float tu2 = (u1 + m_textureWidth - 1) / (256.0f-1.0f);
    Float tv2 = (v1 + m_textureHeight - 1) / (256.0f-1.0f);

    m_d3dDevice->SetTexture(0, m_texture);
            
    // Translate the billboard into place
    D3DXMATRIX mat = control3D->viewMatrix( );
    D3DXMatrixInverse(&mat, NULL, &(control3D->viewMatrix( )));
    mat._41 = fX;
    mat._42 = fY;
    mat._43 = fZ;
    m_d3dDevice->SetTransform(D3DTS_WORLD, &mat);

    DWORD color = sprite->m_color;

    // Enable alpha blending and testing
    m_d3dDevice->SetRenderState(D3DRS_ALPHABLENDENABLE, TRUE);
    m_d3dDevice->SetRenderState(D3DRS_ALPHATESTENABLE,  TRUE);
    m_d3dDevice->SetRenderState(D3DRS_ALPHAREF,         0x08);
    m_d3dDevice->SetRenderState(D3DRS_ALPHAFUNC, D3DCMP_GREATEREQUAL);

    m_d3dDevice->SetRenderState(D3DRS_SRCBLEND,  D3DBLEND_SRCALPHA);
    m_d3dDevice->SetRenderState(D3DRS_DESTBLEND, D3DBLEND_INVSRCALPHA);
    m_d3dDevice->SetRenderState(D3DRS_ZENABLE,      TRUE);
    m_d3dDevice->SetRenderState(D3DRS_ZWRITEENABLE, TRUE);

    SPRITEVERTEX* v;
    m_vertexBuffer->Lock(0, 0, (BYTE**)&v, 0);
    v[0].p = D3DXVECTOR3(x1,y1,0); v[0].color=color; v[0].tu=tu1; v[0].tv=tv2;
    v[1].p = D3DXVECTOR3(x1,y2,0); v[1].color=color; v[1].tu=tu1; v[1].tv=tv1;
    v[2].p = D3DXVECTOR3(x2,y1,0); v[2].color=color; v[2].tu=tu2; v[2].tv=tv2;
    v[3].p = D3DXVECTOR3(x2,y2,0); v[3].color=color; v[3].tu=tu2; v[3].tv=tv1;
    m_vertexBuffer->Unlock();

    // Render the billboarded sprite
    m_d3dDevice->SetVertexShader(D3DFVF_SPRITEVERTEX);
    m_d3dDevice->SetStreamSource(0, m_vertexBuffer, sizeof(SPRITEVERTEX));
    m_d3dDevice->DrawPrimitive(D3DPT_TRIANGLESTRIP, 0, 2);
}




D3DSprite::D3DSprite(Vector3 position, Vector3 velocity) :
    m_next(0), m_prev(0),    
    m_visible(true),
    m_position(position), m_velocity(velocity),
    m_size(0.1f),
    m_nFramesPerLine(1),
    m_currentFrame(0),
    m_maxFrame(0),
    m_delay(0.0f),    
    m_color(0xffffffff),
    m_textureOffsetX(0),
    m_textureOffsetY(0),
    m_textureWidth(0),
    m_textureHeight(0) 
{
    m_textureWidth = 32;
    m_textureHeight = 32;
}


D3DSprite::~D3DSprite( )
{


}

/*

void
D3DSprite::render(LPDIRECT3DDEVICE8 d3dDevice)
{
    Float fX = m_position.x;
    Float fY = m_position.y;
    Float fZ = m_position.z;
    
    Float x1 = -m_size;
    Float x2 =  m_size;
    Float y1 = -m_size;
    Float y2 =  m_size;

    Float u1 = (Float)(m_textureOffsetX + m_textureWidth *(((UInt)m_currentFrame)%m_framesPerLine));
    Float v1 = (Float)(m_textureOffsetY + m_textureHeight*(((UInt)m_currentFrame)/m_framesPerLine));

    Float tu1 = u1 / (256.0f-1.0f);
    Float tv1 = v1 / (256.0f-1.0f);
    Float tu2 = (u1 + m_textureWidth - 1) / (256.0f-1.0f);
    Float tv2 = (v1 + m_textureHeight - 1) / (256.0f-1.0f);

    // Set the game texture
    switch( pObject->dwType )
    {
        case OBJ_DONUT:
        case OBJ_CUBE:
        case OBJ_SPHERE:
            m_pd3dDevice->SetTexture( 0, m_pGameTexture1 );
            break;
        case OBJ_PYRAMID:
        case OBJ_CLOUD:
            m_pd3dDevice->SetTexture( 0, m_pGameTexture2 );
            break;
    }

    // Translate the billboard into place
    D3DXMATRIX mat = m_Camera.GetBillboardMatrix();
    mat._41 = fX;
    mat._42 = fY;
    mat._43 = fZ;
    m_pd3dDevice->SetTransform( D3DTS_WORLD, &mat );

    DWORD dwColor = pSprite->dwColor;

    m_pd3dDevice->SetRenderState( D3DRS_SRCBLEND,  D3DBLEND_SRCALPHA );
    m_pd3dDevice->SetRenderState( D3DRS_DESTBLEND, D3DBLEND_INVSRCALPHA );
    m_pd3dDevice->SetRenderState( D3DRS_ZENABLE,      TRUE );
    m_pd3dDevice->SetRenderState( D3DRS_ZWRITEENABLE, TRUE );

    FLOAT h = 300.0f*pObject->vPos.z + 0.1f;

    SPRITEVERTEX* v;
    m_pSpriteVB->Lock( 0, 0, (BYTE**)&v, 0 );
    v[0].p = D3DXVECTOR3(x1,y1+h,0); v[0].color=dwColor; v[0].tu=tu1; v[0].tv=tv2;
    v[1].p = D3DXVECTOR3(x1,y2+h,0); v[1].color=dwColor; v[1].tu=tu1; v[1].tv=tv1;
    v[2].p = D3DXVECTOR3(x2,y1+h,0); v[2].color=dwColor; v[2].tu=tu2; v[2].tv=tv2;
    v[3].p = D3DXVECTOR3(x2,y2+h,0); v[3].color=dwColor; v[3].tu=tu2; v[3].tv=tv1;
    m_pSpriteVB->Unlock();

    // Render the billboarded sprite
    m_d3dDevice->SetVertexShader(D3DFVF_SPRITEVERTEX);
    m_d3dDevice->SetStreamSource(0, m_pSpriteVB, sizeof(SPRITEVERTEX) );
    m_d3dDevice->DrawPrimitive( D3DPT_TRIANGLESTRIP, 0, 2 );
}
*/

}; // namespace DirectX
