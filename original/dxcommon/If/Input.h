/**
* DXCommon library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __DXCOMMON_INPUT_H__
#define __DXCOMMON_INPUT_H__

// Microsoft apparently didn't define this, so let's do it ourselves
#define DIRECTINPUT_VERSION   0x0800

#include <DxCommon/If/Internal.h>
#include <dinput.h>

#include <vector>


namespace DirectX
{

class Input;
class Keyboard;
class Joystick;
class InputManager;


class Input
{
public:
    _dxcommon_ struct State
    {
        Int     x;
        Int     y;
        Int     z;
        Int     rx;
        Int     ry;
        Int     rz;
        Int     slider1;
        Int     slider2;
        Boolean b1;
        Boolean b2;
        Boolean b3;
        Boolean b4;
        Boolean b5;
        Boolean b6;
        Boolean b7;
        Boolean b8;
        Boolean b9;
        Boolean b10;
        Boolean b11;
        Boolean b12;
        Boolean b13;
        Boolean b14;
        Boolean b15;
        Boolean b16;
        Boolean pov1;
        Boolean pov2;
        Boolean pov3;
        Boolean pov4;
        Boolean pov5;
        Boolean pov6;
        Boolean pov7;
        Boolean pov8;
        Byte    keys[256];
    };

    _dxcommon_ enum
    {
        none     = 0x0000,
        Xaxis    = 0x0001,
        Yaxis    = 0x0002,
        Zaxis    = 0x0004,
        RXaxis   = 0x0008,
        RYaxis   = 0x0010,
        RZaxis   = 0x0020,
        button01 = 0x0040,
        button02 = 0x0080,
        button03 = 0x0100,
        button04 = 0x0200,
        button05 = 0x0400,
        button06 = 0x0800,
        button07 = 0x1000,
        button08 = 0x2000,
        button09 = 0x4000,
        button10 = 0x8000,
        button11 = 0x10000,
        button12 = 0x20000,
        button13 = 0x40000,
        button14 = 0x80000,
        button15 = 0x100000,
        button16 = 0x200000,
        slider   = 0x400000,
        pov      = 0x800000
    };
public:
    _dxcommon_ virtual Int          update( )       { return dxSuccess; }
    _dxcommon_ virtual const State& state( ) const  { return m_state;   }

public:
    virtual Int         dataFormat( )   { return dxSuccess; }
    virtual Int         cooperativeLevel(/* ::Window::Handle handle, UInt flags */) { return dxSuccess; }

protected:
    State               m_state;
};


class Keyboard : public Input
{
public:
    _dxcommon_ Keyboard(LPDIRECTINPUTDEVICE8 device);
    _dxcommon_ virtual ~Keyboard( );

public:
    _dxcommon_ virtual Int         update( );
    

public:
    virtual Int         dataFormat( );
    virtual Int         cooperativeLevel(::Window::Handle handle, UInt flags);

private:
    LPDIRECTINPUTDEVICE8 m_device;
    Int                  m_available;
    Byte                 m_keys[256];
};

class Joystick : public Input
{
public:
    _dxcommon_ Joystick(LPDIRECTINPUTDEVICE8 device);
    _dxcommon_ virtual ~Joystick( );

public:
    _dxcommon_ virtual      Int          update( );
    
public:
    Int   setRange(UInt axis, Int min, Int max);
    void  addAvailable(Int data)               { m_available |= data; }
    virtual Int   dataFormat( );
    virtual Int   cooperativeLevel(::Window::Handle handle /*, UInt flags */);
    void  device(LPDIRECTINPUTDEVICE8 device)   { m_device = device; }
    LPDIRECTINPUTDEVICE8 device( )             { return m_device;   }

    void    FFcapable(Boolean b)                { m_forceFeedbackCapable = b;       }
    Boolean FFcapable( )                        { return m_forceFeedbackCapable;    }
    void    autocenter(Boolean b);

private:
    LPDIRECTINPUTDEVICE8 m_device;
    Int                  m_available;
    Boolean              m_forceFeedbackCapable;
};



class InputManager
{
public:
    _dxcommon_ InputManager( );
    _dxcommon_ virtual ~InputManager( );

public:
    _dxcommon_ Int  initialize(Window::Handle handle);
    _dxcommon_ void finalize( );

    _dxcommon_ Joystick* joystick( )       { return m_joystick;    }
    _dxcommon_ Keyboard* keyboard( )       { return m_keyboard;    }

    _dxcommon_ Int                update( );
    _dxcommon_ const Input::State state( );

private:
    static BOOL CALLBACK    enumJoysticksCallback(const DIDEVICEINSTANCE* pdidInstance, VOID* pContext);
    static BOOL CALLBACK    enumObjectsCallback(const DIDEVICEOBJECTINSTANCE* pdidoi, VOID* pContext);

    static BOOL CALLBACK    enumKeyboardsCallback(const DIDEVICEINSTANCE* pdidInstance, VOID* pContext);

private:
    LPDIRECTINPUT8       directInput( )    { return m_directInput; }
    ::Window::Handle     handle( )         { return m_handle;      }

private:
    Boolean              m_initialized;

    LPDIRECTINPUT8       m_directInput;         
    Joystick*            m_joystick;
    Keyboard*            m_keyboard;
    ::Window::Handle     m_handle;
};


class ForceFeedbackEffect
{
public:
    _dxcommon_ ForceFeedbackEffect(Joystick* joystick);
    _dxcommon_ ForceFeedbackEffect(Joystick* joystick, char* filename);
    _dxcommon_ virtual ~ForceFeedbackEffect( );

public:
    static BOOL CALLBACK EnumAndCreateEffectsCallback(LPCDIFILEEFFECT effect, VOID* classRef);

public:
    _dxcommon_ void play( );
    _dxcommon_ void stop( );

    _dxcommon_ void gain(UInt gain);

public:
    _dxcommon_ void addFileEffect(LPCDIFILEEFFECT fileEffect);

public:
    Joystick*   joystick( )         { return m_joystick;    }
   
private:
    typedef std::vector<LPDIRECTINPUTEFFECT>    EffectVector;
    EffectVector                            m_effects;
    Joystick*                               m_joystick;
};



} // namespace DirectX


#endif /* __DXCOMMON_INPUT_H__ */
