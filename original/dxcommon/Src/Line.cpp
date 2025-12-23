/**
* DXCommon library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#include <DxCommon/If/Line.h>

namespace DirectX
{

Line::Line(LPDIRECT3DDEVICE8 d3dDevice, Vector3 from, Vector3 to, UInt color) :
    m_vertexBuffer(0)
{
    if (FAILED(d3dDevice->CreateVertexBuffer(2*sizeof(Vertex), D3DUSAGE_DONOTCLIP | D3DUSAGE_DYNAMIC | D3DUSAGE_WRITEONLY, D3DFVF_XYZ | D3DFVF_DIFFUSE, D3DPOOL_DEFAULT, &m_vertexBuffer)))
	{
        DXCOMMON("(!) Line : failed to create vertexbuffer");
    }
    else
    {
        Byte*    buffer;
        if (FAILED(m_vertexBuffer->Lock(0, 2*sizeof(Vertex), (BYTE **)&buffer, D3DLOCK_DISCARD | D3DLOCK_NOSYSLOCK)))
    	{ 
            DXCOMMON("(!) Line : failed to lock vertexbuffer");
        }
        else
        {
            Vertex* vertices = reinterpret_cast<Vertex*>(buffer);
	        // fill the buffer with the vertices
	        vertices[0].x = from.x; vertices[0].y = from.y; vertices[0].z = from.z;
            vertices[1].x = to.x;   vertices[1].y = to.y;   vertices[1].z = to.z;
            vertices[0].color = color;
            vertices[1].color = color;
            m_vertexBuffer->Unlock();
        }
    }
}

Line::~Line( )
{
    SAFE_RELEASE(m_vertexBuffer);
}



void Line::render(LPDIRECT3DDEVICE8 d3dDevice)
{
    d3dDevice->SetStreamSource(0, m_vertexBuffer, sizeof(Vertex));
	d3dDevice->SetVertexShader(D3DFVF_XYZ | D3DFVF_DIFFUSE);
	d3dDevice->DrawPrimitive(D3DPT_LINESTRIP /*D3DPT_LINELIST*/, 0, 2);
}

} // namespace DirectX
