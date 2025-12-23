/**
* Common library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __COMMON_TLIST_H__
#define __COMMON_TLIST_H__

template <class Type>
class TList
{
public:
    TList( );
    virtual ~TList( );

public:
    virtual void push(Type*);
    virtual void purge(Type*);
    virtual Type* next(Type*);
    
private:
    Type*     m_first;
    Type*     m_last;
};



template <class Type> TList<Type>::TList( ) :
    m_first(0),
    m_last(0)
{
    COMMON("(+) TList");
}


template <class Type> TList<Type>::~TList( )
{
    COMMON("(-) TList");
    Type* t1 = m_first;
    Type* t2;
    while (t2 = next(t1))
    {
        purge(t1);
        t1 = t2;
    }
}


template <class Type> void TList<Type>::push(Type* t)
{
    t->next = m_first;
    t->previous = 0;
    if (m_first)
        m_first->previous = t;
    m_first = t;
}


template <class Type> void TList<Type>::purge(Type* t)
{
    if (t->previous)
        (t->previous)->next = t->next;
    else
        m_first = t->next;
    if (t->next)
        (t->next)->previous = t->previous;
    t->previous = 0;
    t->next     = 0;
}


template <class Type> Type* TList<Type>::next(Type* t)
{
    if (t)
        return t->next;
    else
        return m_first;
}


#endif /* __COMMON_TLIST_H__ */
