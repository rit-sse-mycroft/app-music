using SpotiFire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Music
{
    enum Mode {Repeat, RepeateOne, Normal};
    class TrackManager
    {
        private List<Track> tracks;
        private Mode currentMode;

        public TrackManager()
        {
            tracks = new List<Track>();
            currentMode = Mode.Normal;
        }

        public void AddTrack(Track track)
        {
            tracks.Add(track);
        }

        public Track PlayTrack(Track track)
        {
            AddTrack(track);
            return Dequeue();
        }

        public async void AddAlbum(Album album)
        {
            AlbumBrowse b = await album.Browse();
            IList<Track> t = b.Tracks;
            tracks.AddRange(t);
        }

        public Track PlayAlbum(Album album)
        {
            AddAlbum(album);
            return Dequeue();
        }

        public Track Dequeue()
        {
            Track track = tracks[0];
            tracks.RemoveAt(0);
            switch (currentMode)
            {
                case Mode.Normal:
                    break;
                case Mode.Repeat:
                    AddTrack(track);
                    break;
                case Mode.RepeateOne:
                    tracks.Insert(0, track);
                    break;
            }
            return track;
        }
    }
}
