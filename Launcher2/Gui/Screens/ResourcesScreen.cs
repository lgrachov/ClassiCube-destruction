﻿// ClassicalSharp copyright 2014-2016 UnknownShadow200 | Licensed under MIT
using System;
using System.Drawing;
using System.IO;
using ClassicalSharp;
using ClassicalSharp.Network;

namespace Launcher {
	
	public sealed class ResourcesScreen : LauncherScreen {
		
		Font infoFont, statusFont;
		ResourcesView view;
		public ResourcesScreen( LauncherWindow game ) : base( game ) {
			game.Window.Mouse.Move += MouseMove;
			game.Window.Mouse.ButtonDown += MouseButtonDown;
			
			textFont = new Font( game.FontName, 16, FontStyle.Bold );
			infoFont = new Font( game.FontName, 14, FontStyle.Regular );
			statusFont = new Font( game.FontName, 13, FontStyle.Italic );
			buttonFont = textFont;
			view = new ResourcesView( game );
			widgets = view.widgets;
		}

		public override void Init() {
			view.Init();
			MakeWidgets();
			Resize();
		}
		
		bool failed;
		public override void Tick() {
			if( fetcher == null || failed ) return;
			CheckCurrentProgress();
			
			if( !fetcher.Check( SetStatus ) )
				failed = true;
			
			if( fetcher.Done ) {
				if( !fetcher.defaultZipExists ) {
					ResourcePatcher patcher = new ResourcePatcher( fetcher );
					patcher.Run();
				}
				fetcher = null;
				GC.Collect();
				game.TryLoadTexturePack();
				GotoNextMenu();
			}
		}
		
		public override void Resize() {
			MakeWidgets();
			view.DrawAll();
			Dirty = true;
		}
		
		void CheckCurrentProgress() {
			Request item = fetcher.downloader.CurrentItem;
			if( item == null ) {
				view.lastProgress = int.MinValue; return;
			}
			
			int progress = fetcher.downloader.CurrentItemProgress;
			if( progress == view.lastProgress ) return;
			view.lastProgress = progress;
			SetFetchStatus( progress );
		}
		
		void SetFetchStatus( int progress ) {
			if( progress >= 0 && progress <= 100 ) {
				view.DrawProgressBox( progress );
				Dirty = true;
			}
		}
		
		ResourceFetcher fetcher;
		Font textFont;
		static FastColour backCol = new FastColour( 120, 85, 151 );
		static readonly string mainText = "Some required resources weren't found" +
			Environment.NewLine + "Okay to download them?";
		static readonly string format = "&eDownload size: {0} megabytes";
		static FastColour clearCol = new FastColour( 12, 12, 12 );
		bool useStatus;
		
		void MakeWidgets() {
			widgetIndex = 0;
			if( useStatus ) {
				MakeLabelAt( widgets[0].Text, statusFont, Anchor.Centre, Anchor.Centre, 0, -10 );
			} else {
				float dataSize = game.fetcher.DownloadSize;
				string text = String.Format( format, dataSize.ToString( "F2" ) );
				MakeLabelAt( text, statusFont, Anchor.Centre, Anchor.Centre, 0, 10 );
			}			

			// Clear the entire previous widgets state.
			for( int i = 1; i < widgets.Length; i++ ) {
				widgets[i] = null;
				selectedWidget = null;
				lastClicked = null;
			}
			
			if( fetcher == null ) {
				MakeLabelAt( mainText, infoFont, Anchor.Centre, Anchor.Centre, 0, -40 );
				MakeButtonAt( "Yes", 70, 35, textFont, Anchor.Centre,
				             -70, 45, DownloadResources );
				
				MakeButtonAt( "No", 70, 35, textFont, Anchor.Centre,
				             70, 45, (x, y) => GotoNextMenu() );
			} else {
				MakeButtonAt( "Cancel", 120, 35, textFont, Anchor.Centre,
				             0, 45, (x, y) => GotoNextMenu() );
			}
			
			if( view.lastProgress >= 0 && view.lastProgress <= 100 ) {
				view.DrawProgressBox( view.lastProgress );
				Dirty = true;
			}
		}
		
		void DownloadResources( int mouseX, int mouseY ) {
			if( game.Downloader == null )
				game.Downloader = new AsyncDownloader( "null" );
			if( fetcher != null ) return;
			
			fetcher = game.fetcher;
			fetcher.DownloadItems( game.Downloader, SetStatus );
			selectedWidget = null;
			Resize();
		}
		
		void GotoNextMenu() {
			if( File.Exists( "options.txt" ) )
				game.SetScreen( new MainScreen( game ) );
			else
				game.SetScreen( new ChooseModeScreen( game, true ) );
		}
		
		void SetStatus( string text ) {
			useStatus = true;
			LauncherLabelWidget widget = (LauncherLabelWidget)widgets[0];
			using( drawer ) {
				drawer.SetBitmap( game.Framebuffer );
				drawer.Clear( backCol, widget.X, widget.Y, widget.Width, widget.Height );
				widget.SetDrawData( drawer, text, statusFont, Anchor.Centre, Anchor.Centre, 0, -10 );
				widget.Redraw( drawer );
				Dirty = true;
			}
		}
		
		public override void Dispose() {
			game.Window.Mouse.Move -= MouseMove;
			game.Window.Mouse.ButtonDown -= MouseButtonDown;
			
			textFont.Dispose();
			infoFont.Dispose();
			statusFont.Dispose();
		}
	}
}
