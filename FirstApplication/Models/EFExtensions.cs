using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Chwinock.Models
{
    public partial class Report
    {
        public decimal? AverageRating{
            get
            {
                decimal? totalRating = 0;
                decimal? numRatings = this.Ratings.Count();

                if (numRatings == 0)
                {
                    return (decimal?)1.5;
                }

                foreach (var r in this.Ratings)
                {
                    totalRating += r.Rank;
                }


                return totalRating / numRatings;
            }
            set
            {
            }
        }
    }
}