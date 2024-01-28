﻿using FoxDb.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class MovePlaylistItemsTask : PlaylistTaskBase
    {
        public MovePlaylistItemsTask(Playlist playlist, int sequence, IEnumerable<PlaylistItem> playlistItems)
            : base(playlist, sequence)
        {
            this.PlaylistItems = playlistItems;
        }

        public IEnumerable<PlaylistItem> PlaylistItems { get; private set; }

        protected override async Task OnRun()
        {
            var moveItems = new List<PlaylistItem>();
            var copyItems = new List<PlaylistItem>();
            foreach (var playlistItem in this.PlaylistItems)
            {
                if (playlistItem.Playlist_Id == this.Playlist.Id)
                {
                    moveItems.Add(playlistItem);
                }
                else
                {
                    copyItems.Add(playlistItem);
                }
            }
            if (moveItems.Count > 0)
            {
                await this.MoveItems(moveItems).ConfigureAwait(false);
            }
            if (copyItems.Count > 0)
            {
                await this.AddPlaylistItems(copyItems).ConfigureAwait(false);
                await this.ShiftItems(QueryOperator.GreaterOrEqual, this.Sequence, this.Offset).ConfigureAwait(false);
                await this.SetPlaylistItemsStatus(PlaylistItemStatus.None).ConfigureAwait(false);
            }
        }

        protected override async Task OnCompleted()
        {
            await base.OnCompleted().ConfigureAwait(false);
            await this.SignalEmitter.Send(new Signal(this, CommonSignals.PlaylistUpdated, new[] { this.Playlist })).ConfigureAwait(false);
        }
    }
}
