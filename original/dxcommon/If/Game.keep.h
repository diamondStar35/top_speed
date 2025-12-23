/**
* DXCommon library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __DXCOMMON_GAME_H__
#define __DXCOMMON_GAME_H__

#include <DxCommon/If/Internal.h>
#include <DxCommon/If/Input.h>
#include <DxCommon/If/D3DFont.h>
#include <DxCommon/If/Timer.h>

#include <D3DX8.h>

namespace DirectX
{

struct D3DModeInfo
{
    DWORD      Width;      // Screen width in this mode
    DWORD      Height;     // Screen height in this mode
    D3DFORMAT  Format;     // Pixel format in this mode
    DWORD      dwBehavior; // Hardware / Software / Mixed vertex processing
    D3DFORMAT  DepthStencilFormat; // Which depth/stencil format to use with this mode
};

struct D3DDeviceInfo
{
    // Device data
    D3DDEVTYPE   DeviceType;      // Reference, HAL, etc.
    D3DCAPS8     d3dCaps;         // Capabilities of this device
    const TCHAR* strDesc;         // Name of this device
    BOOL         bCanDoWindowed;  // Whether this device can work in windowed mode

    // Modes for this device
    DWORD        dwNumModes;
    D3DModeInfo  modes[150];

    // Current state
    DWORD        dwCurrentMode;
    BOOL         bWindowed;
    D3DMULTISAMPLE_TYPE MultiSampleTypeWindowed;
    D3DMULTISAMPLE_TYPE MultiSampleTypeFullscreen;
};

struct D3DAdapterInfo
{
    // Adapter data
    D3DADAPTER_IDENTIFIER8 d3dAdapterIdentifier;
    D3DDISPLAYMODE d3ddmDesktop;      // Desktop display mode for this adapter

    // Devices for this adapter
    DWORD          dwNumDevices;
    D3DDeviceInfo  devices[5];

    // Current state
    DWORD          dwCurrentDevice;
};


/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
class Game
{
public:
    _dxcommon_ Game( );
    _dxcommon_ virtual ~Game( );

public:
    _dxcommon_ virtual Int   create(Window::Instance, Int icon);

    _dxcommon_ virtual Int   initialize( )                         { return dxSuccess; }
    _dxcommon_ virtual void  finalize( )                           {                   }

    _dxcommon_ virtual Int   run(Int keyboardAccelerator);

    _dxcommon_ virtual Int   prepareNextFrame(Float elapsed)       { return dxSuccess; }
    _dxcommon_ virtual Int   render(Float elapsed)                 { return dxSuccess; }

    _dxcommon_ virtual Int   displayMode(Int width, Int height, Boolean fullscreen, 
                                         Boolean antialiasing = false);


    BOOL findDepthStencilFormat( UINT iAdapter, D3DDEVTYPE DeviceType, D3DFORMAT TargetFormat, D3DFORMAT* pDepthStencilFormat );
    Int buildDeviceList();
    Int initialize3DEnvironment();
    HRESULT ConfirmDevice(D3DCAPS8*,DWORD,D3DFORMAT)   { return S_OK; }
    _dxcommon_ const UInt  numberOfAdapters( ) { return m_dwNumAdapters; }
    _dxcommon_ const Char* adapterDescription(UInt adapter)  { return m_Adapters[adapter].d3dAdapterIdentifier.Description; }
    _dxcommon_ const UInt  numberOfModes(UInt adapter);
    _dxcommon_ void        modeDescription(UInt adapter, UInt mode, char* desc);

    

protected:
    LPDIRECT3D8       m_d3d;              // The main D3D object
    D3DAdapterInfo    m_Adapters[10];
    DWORD             m_dwNumAdapters;
    BOOL              m_bUseDepthBuffer;   // Whether to autocreate depthbuffer
    DWORD             m_dwMinDepthBits;    // Minimum number of bits needed in depth buffer
    DWORD             m_dwMinStencilBits;  // Minimum number of bits needed in stencil buffer
    DWORD             m_dwAdapter;
    BOOL              m_bWindowed;
    D3DPRESENT_PARAMETERS m_d3dpp; 
    D3DCAPS8          m_d3dCaps;   
    DWORD             m_dwCreateFlags;  
    TCHAR             m_strDeviceStats[90];
    D3DSURFACE_DESC   m_d3dsdBackBuffer; 

public:
    _dxcommon_ virtual void  showFPS(Boolean b)                    { m_showFPS = b; }

    _dxcommon_ const   Int   backgroundColor( ) const              { return m_backgroundColor; }
    _dxcommon_         void  backgroundColor(Int c)                { m_backgroundColor = c; }

    _dxcommon_         void  enableFog(UInt color, Float near, Float far, Float density);
    _dxcommon_         void  disableFog( );

    _dxcommon_ LPDIRECT3DDEVICE8 d3dDevice( )                      { return m_d3dDevice; }

public:
    _dxcommon_ class Control3D
    {
    public: 
        _dxcommon_ Control3D(Game* game, LPDIRECT3DDEVICE8 device, Int screenWidth, Int screenHeight);
        _dxcommon_ virtual ~Control3D( );

    public:
        _dxcommon_ void view(Float fromx, Float fromy, Float fromz,
                             Float tox = 0, Float toy = 0, Float toz = 0);
        _dxcommon_ void view(Vector3 from, Vector3 to);
        _dxcommon_ void projection(Float Near, Float Far);
        _dxcommon_ void projection(Float Near, Float Far, Float angle);
        _dxcommon_ void material(UInt color, Float alpha = 1.0f);
        _dxcommon_ void ambient(UInt color);
        _dxcommon_ void alpha(Boolean enable);
        _dxcommon_ void antialiasing(Boolean enable);
        _dxcommon_ void position(LPDIRECT3DDEVICE8 d3dDevice, Float x, Float y, Float z);
        _dxcommon_ void position(LPDIRECT3DDEVICE8 d3dDevice, Vector3 pos);
        _dxcommon_ void yawPitchRoll(LPDIRECT3DDEVICE8 d3dDevice, Float yaw, Float pitch, Float roll);
        _dxcommon_ void yawPitchRoll(LPDIRECT3DDEVICE8 d3dDevice, Vector3 ypr);
        _dxcommon_ void rotateAxis(LPDIRECT3DDEVICE8 d3dDevice, DirectX::Vector3 axis, Float angle);
        _dxcommon_ void scale(LPDIRECT3DDEVICE8 d3dDevice, Float scale);
        _dxcommon_ void scale(LPDIRECT3DDEVICE8 d3dDevice, Float scaleX, Float scaleY, Float scaleZ);
        _dxcommon_ void reset(LPDIRECT3DDEVICE8 d3dDevice);

        _dxcommon_ D3DXMATRIX& viewMatrix( )   { return m_viewMatrix; }

    protected:
        Game*                   m_game;
        LPDIRECT3DDEVICE8       m_d3dDevice;
        Int                     m_screenWidth;
        Int                     m_screenHeight;
        D3DXMATRIX              m_viewMatrix;
    };

public:
    Control3D*      control3D( )           { return m_control3D;          }
    Input::State    inputState( )          { return m_input->state( );    }
    D3DCAPS8&       capabilities( )        { return m_d3dCapabilities;    }
    Boolean         multisampleCapable( )  { return m_multisampleCapable; }
    D3DFORMAT       textureFormat( )       { return m_d3dTextureFormat;   }
    Int             screenWidth( )         { return m_screenWidth;        }
    Int             screenHeight( )        { return m_screenHeight;       }
    Window::Handle  mainWindow( )          { return m_mainWindow;         }

public:
    LRESULT MsgProc( HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam );

protected:
    _dxcommon_ virtual Int   initializeDisplay( );



protected:
    Control3D*              m_control3D;
    Window::Handle          m_mainWindow;
    Boolean                 m_active;
    Boolean                 m_fullscreen;
    D3DDISPLAYMODE          m_desktopMode;
    Int                     m_screenWidth;
    Int                     m_screenHeight;
    D3DFORMAT               m_d3dFullscreenFormat;        // Pixel format for fullscreen modes
    D3DFORMAT               m_d3dWindowedFormat;
    D3DFORMAT               m_d3dTextureFormat;           // Pixel format for textures
    D3DCAPS8                m_d3dCapabilities;
    Boolean                 m_multisampleCapable;
    HWND                    m_windowHandle;
    
    LPDIRECT3DDEVICE8       m_d3dDevice;           // Class to handle D3D device
    D3DPRESENT_PARAMETERS   m_d3dPresentParameters;

    D3DFont*                m_font;
    InputManager*           m_input;
    Timer                   m_timer;
    Huge                    m_elapsed;

    Boolean                 m_showFPS;
    Int                     m_backgroundColor;
};


} // namespace DirectX



#endif /* __DXCOMMON_GAME_H__ */
