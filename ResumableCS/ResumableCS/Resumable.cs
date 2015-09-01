using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumableCS {
    class Resumable {
        // these parameter might get changed as the developement
        private int maxSize;
        private int chunksize;
        private int thread;

        public Resumable(int maxSize, int chunksize, int thread, string method) {
            this.maxSize = maxSize;
            this.chunksize = chunksize;          
        }
    }
}
