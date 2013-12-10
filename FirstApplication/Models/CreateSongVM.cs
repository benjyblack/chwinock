using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Chwinock.Models
{
    public class CreateSongVM
    {
        // Properties
        public Song Song { get; set; }
        public IEnumerable<Report> Reports { get; set; }

        //Constructors
        public CreateSongVM()
        {
            Song = new Song();
            Reports = new ChwinockEntities().Reports;
        }
        public CreateSongVM(Song song)
        {
            Song = song;
            Reports = new ChwinockEntities().Reports;
        }
    }
}