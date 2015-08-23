# Music Replacer

Music Replacer is an add-on for Kerbal Space Program that provides support for replacing music withn the game via configuration files.  The following is an example configuration with a detailed description of all the attributes that can be used:

<pre>
// The main MUSIC_REPLACER node can be in a .cfg file anywhere within the
// GameData directory.  Multiple MUSIC_REPLACER nodes (within multiple files)
// is allowed - the information will all be merged together.
MUSIC_REPLACER
{
    // Each MUSIC node represents a single music sample to add/replace.
    // Multiple MUSIC nodes may be supplied.
    MUSIC
    {
        // Name of the music to replace.  The spacePlayList is the one that is
        // used for anywhere in flight.
        //
        // Required:  Yes
        // Values:
        //     menuTheme
        //     menuAmbience
        //     credits
        //     trackingAmbience
        //     spaceCenterAmbience
        //     VABAmbience
        //     SPHAmbience
        //     astroComplexAmbience
        //     researchComplexAmbience
        //     missionControlAmbience
        //     adminFacilityAmbience
        //     constructionPlaylist
        //     spacePlaylist
        //
        name = spacePlaylist

        // URL of the music file, relative to the GameData directory.  This
        // should be an Ogg Vorbis encoded file (and should not include the 
        // file extension).
        //
        // Required:  Yes
        //
        musicURL = MusicReplacer/example_music

        // Set this to true to clear music from existing lists 
        // (constructionPlaylist or spacePlaylist).  If this is set for
        // spacePlayList, then recommend
        //
        // Required:  No (defaults to false)
        //
        removeExisting = false


        // The following attributes only apply to the spacePlayList (used
        // everywhere in flight/space).

        // Name of the celestial body for which the music applies to.  If not
        // provided, it is assumed to apply for any celestial body.
        //
        // Required:  No
        //
        celestialBody = Kerbin

        // Minimum altitude for which the music applies.  If below this altitude
        // then either another matching music sample will be played or the if 
        // there are no matches, then something from the default list.
        //
        // Required:  No
        // Default:   0.0
        //
        minAltitude = 1500.0

        // Maximum altitude for which the music applies.  If above this altitude
        // then either another matching music sample will be played or the if 
        // there are no matches, then something from the default list.
        //
        // Required:  No
        // Default:   infinite
        //
        maxAltitude = 150000.0
    }
}
</pre>

Music Replacer is developed by nightingale under the [MIT license](https://raw.githubusercontent.com/jrossignol/MusicReplacer/master/LICENSE.txt).
