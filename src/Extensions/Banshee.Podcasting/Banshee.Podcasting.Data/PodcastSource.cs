/***************************************************************************
 *  PodcastManagerSource.cs
 *
 *  Copyright (C) 2007 Michael C. Urbanski
 *  Written by Mike Urbanski <michael.c.urbanski@gmail.com>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;

using Gtk;
using Gdk;

using Mono.Unix;

using Hyena.Data;
using Hyena.Data.Gui;
using Hyena.Data.Sqlite;

using Banshee.Gui;
using Banshee.Base;
using Banshee.Database;
using Banshee.Collection;
using Banshee.ServiceStack;
using Banshee.Collection.Database;

using Banshee.Sources;
using Banshee.Sources.Gui;

using Banshee.Podcasting.Data;

using Migo.Syndication;

namespace Banshee.Podcasting.Gui
{ 
    public class PodcastSource : Banshee.Library.LibrarySource
    {
        private PodcastUnheardFilterModel unheard_model;
        private PodcastFeedModel feed_model;

        private string baseDirectory;
        public override string BaseDirectory {
            get { return baseDirectory; }
        }

        public override bool CanRename {
            get { return false; }
        }
        
        // FIXME all three of these should be true, eventually
        public override bool CanAddTracks {
            get { return false; }
        }

        public override bool CanRemoveTracks {
            get { return false; }
        }

        public override bool CanDeleteTracks {
            get { return false; }
        }
        
        public PodcastFeedModel FeedModel {
            get { return feed_model; }
        }
        

#region Constructors

        public PodcastSource () : this (null)
        {
        }

        public PodcastSource (string baseDirectory) : base (Catalog.GetString ("Podcasts"), "PodcastLibrary", 100)
        {
            this.baseDirectory = baseDirectory;

            Properties.SetString ("Icon.Name", "podcast-icon-22");
            Properties.SetString ("ActiveSourceUIResource", "ActiveSourceUI.xml");
            Properties.SetString ("GtkActionPath", "/PodcastSourcePopup");
            Properties.Set<ISourceContents> ("Nereid.SourceContents", new PodcastSourceContents ());
            
            Properties.SetString ("TrackView.ColumnControllerXml", String.Format (@"
                    <column-controller>
                      <column>
                          <visible>true</visible>
                          <renderer type=""Banshee.Podcasting.Gui.PodcastItemActivityColumn"" property=""Activity"" />
                          <sort-key>DownloadStatus</sort-key>
                          <width>.025</width>
                          <max-width>30</max-width>
                          <min-width>30</min-width>
                      </column>
                      <add-all-defaults />
                      <remove-default column=""TrackColumn"" />
                      <remove-default column=""DiscColumn"" />
                      <remove-default column=""ComposerColumn"" />
                      <remove-default column=""ArtistColumn"" />
                      <column modify-default=""AlbumColumn"">
                        <title>{0}</title>
                      </column>
                      <column modify-default=""FileSizeColumn"">
                          <visible>true</visible>
                      </column>
                      <column>
                          <visible>true</visible>
                          <title>Published</title>
                          <renderer type=""Banshee.Podcasting.Gui.ColumnCellPublished"" property=""PublishedDate"" />
                          <sort-key>PublishedDate</sort-key>
                      </column>
                      <sort-column direction=""desc"">published_date</sort-column>
                    </column-controller>
                ",
                Catalog.GetString ("Podcast")
            ));
        }
        
#endregion
        
        protected override bool HasArtistAlbum {
            get { return false; }
        }
        
        protected override void InitializeTrackModel ()
        {
            DatabaseTrackModelProvider<PodcastTrackInfo> track_provider =
                new DatabaseTrackModelProvider<PodcastTrackInfo> (ServiceManager.DbConnection);

            DatabaseTrackModel = new PodcastTrackListModel (ServiceManager.DbConnection, track_provider, this);

            TrackCache = new DatabaseTrackModelCache<PodcastTrackInfo> (ServiceManager.DbConnection,
                    UniqueId, track_model, track_provider);
                    
            feed_model = new PodcastFeedModel (this, DatabaseTrackModel, ServiceManager.DbConnection, "PodcastFeeds");
            
            unheard_model = new PodcastUnheardFilterModel (DatabaseTrackModel);
            
            AfterInitialized ();
        }
        
        public override void AddChildSource (Source child)
        {
            Hyena.Log.Information ("Playlists and smart playlists are not supported by the Podcast Library, yet", "", true);
            if (child is IUnmapableSource) {
                (child as IUnmapableSource).Unmap ();
            }
        }
        
        // Probably don't want this -- do we want to allow actually removing the item?  It will be
        // repopulated the next time we update the podcast feed...
        /*protected override void DeleteTrack (DatabaseTrackInfo track)
        {
            PodcastTrackInfo episode = track as PodcastTrackInfo;
            if (episode != null) {
                if (episode.Uri.IsFile)
                    base.DeleteTrack (track);
                
                episode.Delete ();
                episode.Item.Delete (false);
            }
        }*/
        
        /*protected override void AddTrack (DatabaseTrackInfo track)
        {
            // TODO
            // Need to create a Feed, FeedItem, and FeedEnclosure for this track for it to be
            // considered a Podcast item
            base.AddTrack (track);
        }*/
        
        public override System.Collections.Generic.IEnumerable<Banshee.Collection.Database.IFilterListModel> FilterModels {
            get {
                yield return unheard_model;
                yield return feed_model;
            }
        }
        
        public override bool ShowBrowser {
            get { return true; }
        }
        
        /*public override IEnumerable<SmartPlaylistDefinition> DefaultSmartPlaylists {
            get { return default_smart_playlists; }
        }

        private static SmartPlaylistDefinition [] default_smart_playlists = new SmartPlaylistDefinition [] {
            new SmartPlaylistDefinition (
                Catalog.GetString ("Favorites"),
                Catalog.GetString ("Videos rated four and five stars"),
                "rating>=4"),

            new SmartPlaylistDefinition (
                Catalog.GetString ("Unwatched"),
                Catalog.GetString ("Videos that haven't been played yet"),
                "plays=0"),
        };/*

/*
        public new TrackListModel TrackModel {
            get { return null; }
        }

        public override void RemoveSelectedTracks ()
        {
        }

        public override void DeleteSelectedTracks ()
        {
            throw new InvalidOperationException ();
        }

        public override bool CanRemoveTracks {
            get { return false; }
        }

        public override bool CanDeleteTracks {
            get { return false; }
        }

        public override bool ConfirmRemoveTracks {
            get { return false; }
        }
        
        public override bool ShowBrowser {
            get { return false; }
        }
*/
    }
}