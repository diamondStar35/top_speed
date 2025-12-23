/**
* Common library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __COMMON_TQUEUE_H__
#define __COMMON_TQUEUE_H__

template <class Type>
class TQueue
{
public:
    TQueue( );
    virtual ~TQueue( );

public:
    virtual void push(Type);
    virtual Int  purge(Type);
    virtual Type* next(Type);
    
private:
    Type*     m_first;
};



template <class Type> TQueue<Type>::TQueue( ) :
    m_first(0)
{
    COMMON("(+) TQueue");
}


template <class Type> TQueue<Type>::~TQueue( )
{
    COMMON("(-) TQueue");

}




#endif /* __COMMON_TQUEUE_H__ */
