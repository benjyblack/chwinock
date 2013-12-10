using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Chwinock.Models
{
    public class CreateAlcoholVM
    {
        // Properties
        public Alcohol Alcohol { get; set; }
        public IEnumerable<Report> Reports { get; set; }

        //Constructors
        public CreateAlcoholVM()
        {
            Alcohol = new Alcohol();
            Reports = new ChwinockEntities().Reports;
        }
        public CreateAlcoholVM(Alcohol alcohol)
        {
            Alcohol = alcohol;
            Reports = new ChwinockEntities().Reports;
        }
    }
}