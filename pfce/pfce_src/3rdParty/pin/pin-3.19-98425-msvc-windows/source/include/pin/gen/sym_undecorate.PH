#ifndef PIN_G_SYM_UNDECORATE_PH
#define PIN_G_SYM_UNDECORATE_PH

/*! @file
    Definitions for symbol undecoration
*/

/*! @ingroup SYMBOLS
    List of supported symbol name undecoration styles.
*/
enum UNDECORATION
{
    UNDECORATION_COMPLETE, ///< Undecorate to full symbol signature
    UNDECORATION_NAME_ONLY ///< Undecorate to [scope::]name
};

#endif // PIN_G_SYM_UNDECORATE_PH
