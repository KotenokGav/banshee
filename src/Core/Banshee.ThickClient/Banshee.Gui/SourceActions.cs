//
// SourceActions.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Mono.Unix;
using Gtk;

using Hyena;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Configuration;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Playlist;
using Banshee.Playlist.Gui;
using Banshee.Playlists.Formats;
using Banshee.SmartPlaylist;
using Banshee.Gui.Dialogs;

namespace Banshee.Gui
{
    public class SourceActions : BansheeActionGroup
    {
        private InterfaceActionService action_service;

        private IHasSourceView source_view;
        public IHasSourceView SourceView {
            get { return source_view; }
            set { source_view = value; }
        }

        public Source ActionSource {
            get { return SourceView.HighlightedSource ?? ServiceManager.SourceManager.ActiveSource; }
        }
        
        public SourceActions (InterfaceActionService actionService) : base ("Source")
        {
            action_service = actionService;

            Add (new ActionEntry [] {
                new ActionEntry ("NewPlaylistAction", Stock.New,
                    Catalog.GetString ("_New Playlist"), "<control>N",
                    Catalog.GetString ("Create a new empty playlist"), OnNewPlaylist),

                new ActionEntry ("NewSmartPlaylistAction", null,
                    Catalog.GetString ("New _Smart Playlist"), null,
                    Catalog.GetString ("Create a new smart playlist"), OnNewSmartPlaylist),

                new ActionEntry ("NewSmartPlaylistFromSearchAction", null,
                    Catalog.GetString ("New _Smart Playlist _From Search"), null,
                    Catalog.GetString ("Create a new smart playlist from the current search"), OnNewSmartPlaylistFromSearch),

                new ActionEntry ("SourceContextMenuAction", null, 
                    String.Empty, null, null, OnSourceContextMenu),

                new ActionEntry ("ImportSourceAction", null,
                    Catalog.GetString ("Import Source"), null,
                    Catalog.GetString ("Import source to library"), OnImportSource),

                new ActionEntry ("RenameSourceAction", "gtk-edit", 
                    "Rename", "F2", "Rename", OnRenameSource),

                new ActionEntry ("ExportPlaylistAction", null,
                    Catalog.GetString ("Export Playlist..."), null,
                    Catalog.GetString ("Export a playlist"), OnExportPlaylist),

                new ActionEntry ("UnmapSourceAction", Stock.Delete,
                    "Unmap", "<shift>Delete", null, OnUnmapSource),
                    
                new ActionEntry ("SourcePropertiesAction", Stock.Properties,
                    "Source Properties", null, null, OnSourceProperties),
            });

            AddImportant (
                new ActionEntry ("RefreshSmartPlaylistAction", Stock.Refresh,
                    Catalog.GetString ("Refresh"), null,
                    Catalog.GetString ("Refresh this randomly sorted smart playlist"), OnRefreshSmartPlaylist)
            );
            
            Add (new Gtk.ActionEntry [] {
                new Gtk.ActionEntry ("SortChildrenAction", null, 
                    Catalog.GetString ("Sort Children"), null, null, null),
                    
                new Gtk.ActionEntry ("SortChildrenNameAscAction", null, 
                    Catalog.GetString ("Name Ascending"), null, null, 
                    delegate { ActionSource.SortChildSources (new Source.NameComparer (), true); }),
                    
                new Gtk.ActionEntry ("SortChildrenNameDescAction", null, 
                    Catalog.GetString ("Name Descending"), null, null, 
                    delegate { ActionSource.SortChildSources (new Source.NameComparer (), false); }),
                    
                new Gtk.ActionEntry ("SortChildrenSizeAscAction", null, 
                    Catalog.GetString ("Size Ascending"), null, null, 
                    delegate { ActionSource.SortChildSources (new Source.SizeComparer (), true); }),
                    
                new Gtk.ActionEntry ("SortChildrenSizeDescAction", null, 
                    Catalog.GetString ("Size Descending"), null, null, 
                    delegate { ActionSource.SortChildSources (new Source.SizeComparer (), false); })
            });
                
            //ServiceManager.SourceManager.SourceUpdated += OnPlayerEngineStateChanged;
            //ServiceManager.SourceManager.SourceViewChanged += OnPlayerEngineStateChanged;
            //ServiceManager.SourceManager.SourceAdded += OnPlayerEngineStateChanged;
            //ServiceManager.SourceManager.SourceRemoved += OnPlayerEngineStateChanged;
            ServiceManager.SourceManager.ActiveSourceChanged += HandleActiveSourceChanged;
            action_service.GlobalActions["EditMenuAction"].Activated += HandleEditMenuActivated;
        }
            
#region State Event Handlers

        private void HandleActiveSourceChanged (SourceEventArgs args)
        {
            UpdateActions ();
        }

        private void HandleEditMenuActivated (object sender, EventArgs args)
        {
            UpdateActions ();
        }

#endregion

#region Action Event Handlers

        private void OnNewPlaylist (object o, EventArgs args)
        {
            PlaylistSource playlist = new PlaylistSource ("New Playlist");
            playlist.Save ();
            ServiceManager.SourceManager.DefaultSource.AddChildSource (playlist);
            playlist.NotifyUpdated ();
        }

        private void OnNewSmartPlaylist (object o, EventArgs args)
        {
            Editor ed = new Editor ();
            ed.RunDialog ();
            //playlist.NotifyUpdated ();
        }

        private void OnNewSmartPlaylistFromSearch (object o, EventArgs args)
        {
            // TODO create playlist and save it
            
            //playlist.NotifyUpdated ();

            // TODO should begin editing the name after making it, but this changed
            // the ActiveSource to the new playlist and we don't want that.
            //SourceView.BeginRenameSource (playlist);
        }

        private void OnSourceContextMenu (object o, EventArgs args)
        {
            UpdateActions ();

            string path = ActionSource.Properties.GetString ("GtkActionPath") ?? "/SourceContextMenu";
            Gtk.Menu menu = action_service.UIManager.GetWidget (path) as Menu;
            if (menu == null) {
                return;
            }

            menu.Show (); 
            menu.Popup (null, null, null, 0, Gtk.Global.CurrentEventTime);
            menu.SelectionDone += delegate {
                SourceView.ResetHighlight ();
                UpdateActions ();
            };
        }
            
        private void OnImportSource (object o, EventArgs args)
        {
        }

        private void OnRenameSource (object o, EventArgs args)
        {
            SourceView.BeginRenameSource (ActionSource);
        }

        private void OnExportPlaylist (object o, EventArgs args)
        {
            AbstractPlaylistSource source = ActionSource as AbstractPlaylistSource;
            if (source == null) {
                return;
            }

            PlaylistExportDialog chooser = new PlaylistExportDialog (source.Name, PrimaryWindow);

            string uri = null;
            PlaylistFormatDescription format = null;
            int response = chooser.Run ();            
            if (response == (int) ResponseType.Ok) {                    
                uri = chooser.Uri;
                // Get the format that the user selected.
                format = chooser.GetExportFormat ();
            }             
            chooser.Destroy (); 

            if (uri == null) {
                // User cancelled export.
                return;
            }
            
            try {
                IPlaylistFormat playlist = (IPlaylistFormat)Activator.CreateInstance (format.Type);
                SafeUri suri = new SafeUri (uri);
                if (suri.IsLocalPath) {
                    playlist.BaseUri = new Uri (System.IO.Path.GetDirectoryName (suri.LocalPath));
                    Console.WriteLine (playlist.BaseUri.LocalPath);
                }
                playlist.Save (Banshee.IO.File.OpenWrite (new SafeUri (uri), true), source);
            } catch (Exception e) {
                Console.WriteLine (e);
                Log.Error (Catalog.GetString ("Could not export playlist"), e.Message);
            }
        }

        private void OnUnmapSource (object o, EventArgs args)
        {
            IUnmapableSource source = ActionSource as IUnmapableSource;
            if (source != null && source.CanUnmap && (!source.ConfirmBeforeUnmap || ConfirmUnmap (source)))
                source.Unmap ();
        }

        private void OnRefreshSmartPlaylist (object o, EventArgs args)
        {
            SmartPlaylistSource playlist = ActionSource as SmartPlaylistSource;

            if (playlist != null && playlist.CanRefresh) {
                playlist.Reload ();
            }
        }

        private void OnSourceProperties (object o, EventArgs args)
        {
            Source source = ActionSource;
            if (source is SmartPlaylistSource) {
                Editor ed = new Editor (source as SmartPlaylistSource);
                ed.RunDialog ();
            }
        }

#endregion

#region Utility Methods

        private Source last_source = null;
        private void UpdateActions ()
        {
            Source source = ActionSource;

            if (source != last_source && source != null) {
                IUnmapableSource unmapable = source as IUnmapableSource;
                SmartPlaylistSource smart_playlist = source as SmartPlaylistSource;
                UpdateAction ("UnmapSourceAction", unmapable != null, unmapable != null && unmapable.CanUnmap, source);
                UpdateAction ("RenameSourceAction", source.CanRename, true, null);
                UpdateAction ("ImportSourceAction", source is IImportable, true, source);
                UpdateAction ("ExportPlaylistAction", source is AbstractPlaylistSource, true, source);
                UpdateAction ("SourcePropertiesAction", source.HasProperties, true, source);
                UpdateAction ("RefreshSmartPlaylistAction", smart_playlist != null && smart_playlist.CanRefresh, true, source);
                last_source = source;
            }
            
            if (source != null) {
                UpdateAction ("SortChildrenAction", source.Children.Count > 0, true, null);
            }
        }

        private static bool ConfirmUnmap (IUnmapableSource source)
        {
            string key = "no_confirm_unmap_" + source.GetType ().Name.ToLower ();
            bool do_not_ask = ConfigurationClient.Get<bool> ("sources", key, false);
            
            if (do_not_ask) {
                return true;
            }
        
            Banshee.Widgets.HigMessageDialog dialog = new Banshee.Widgets.HigMessageDialog (
                ServiceManager.Get<GtkElementsService> ("GtkElementsService").PrimaryWindow,
                Gtk.DialogFlags.Modal,
                Gtk.MessageType.Question,
                Gtk.ButtonsType.Cancel,
                String.Format (Catalog.GetString ("Are you sure you want to delete this {0}?"),
                    source.GenericName.ToLower ()),
                source.Name);
            
            dialog.AddButton (Gtk.Stock.Delete, Gtk.ResponseType.Ok, false);
            
            Gtk.Alignment alignment = new Gtk.Alignment (0.0f, 0.0f, 0.0f, 0.0f);
            alignment.TopPadding = 10;
            Gtk.CheckButton confirm_button = new Gtk.CheckButton (String.Format (Catalog.GetString (
                "Do not ask me this again"), source.GenericName.ToLower ()));
            confirm_button.Toggled += delegate {
                do_not_ask = confirm_button.Active;
            };
            alignment.Add (confirm_button);
            alignment.ShowAll ();
            dialog.LabelVBox.PackStart (alignment, false, false, 0);
            
            try {
                if (dialog.Run () == (int)Gtk.ResponseType.Ok) {
                    ConfigurationClient.Set<bool> ("sources", key, do_not_ask);
                    return true;
                }
                
                return false;
            } finally {
                dialog.Destroy ();
            }
        }

#endregion
        
    }
}
