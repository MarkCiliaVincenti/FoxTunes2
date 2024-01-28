﻿using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IBassStreamAdvisor : IBaseComponent
    {
        void Advise(IBassStreamProvider provider, PlaylistItem playlistItem, IList<IBassStreamAdvice> advice);
    }
}
