using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using Chwinock.Models;

namespace Chwinock.Controllers
{
    public class HomeController : Controller
    {

        [HttpGet]
        public ActionResult Index()
        {
            return View(new ChwinockEntities()
                .Reports
                .OrderByDescending(x => x.CreateDate));
        }

        [HttpGet]
        public ActionResult About()
        {
            return View();
        }

        [HttpGet]
        public ContentResult ChillNameFor(String userId)
        {
            var db = new ChwinockEntities();

            return Content(db.Users.Single(u => u.UserID == userId).ChillName);
        }

        public ContentResult GenerateChillNameFor(String userId)
        {
            var db = new ChwinockEntities();

            var targetUser = db.Users.Single(u => u.UserID == userId);

            var prevChillName = targetUser.ChillName;
            var newChillName = "";

            do{
                newChillName = Common.Utilities.GenerateName(targetUser.Name, new System.IO.StreamReader(Server.MapPath(@"~/Content/name-list.txt")));
            } while(newChillName == prevChillName);

            targetUser.ChillName = newChillName;

            db.SaveChanges();

            return Content(newChillName);
        }

        [HttpGet]
        public PartialViewResult Report(Guid reportId)
        {
            return PartialView("_Report", new ChwinockEntities()
                .Reports
                .Single(r => r.ReportID == reportId));
        }

        [HttpGet]
        public PartialViewResult Chwinometer(Guid ReportID) 
        {
            var reportChwinometer = new Rating();

            reportChwinometer.ReportID = ReportID;

            return PartialView("_Chwinometer", reportChwinometer);
        }

        [HttpGet]
        public PartialViewResult NewComment(Guid reportId)
        {
            var newComment = new Comment();

            newComment.ReportID = reportId;

            return PartialView("_NewComment", newComment);
        }

        [HttpPost]
        public ActionResult PostComment(Comment model)
        {
            var db = new ChwinockEntities();

            model.CommentID = Guid.NewGuid();
            model.CreateDate = DateTime.UtcNow;

            db.Comments.AddObject(model);

            db.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpGet]
        public PartialViewResult CommentsFor(Guid reportId)
        {
            return PartialView("_Comments", new ChwinockEntities()
                .Comments
                .Where(c => c.ReportID == reportId)
                .OrderBy(c => c.CreateDate));
        }

        public PartialViewResult AlcoholDetails(Guid alcoholId)
        {
            return PartialView("_AlcoholDetails", new ChwinockEntities().Alcohols.Single(a => a.AlcoholID == alcoholId));
        }

        [HttpPost]
        public ActionResult Rate(Rating model)
        {
            var db = new ChwinockEntities();

            // Check to see if User has already rated this report
            if (db.Ratings.Any(x => x.UserID == model.UserID && x.ReportID == model.ReportID))
            {
                var targetRating = db.Ratings.Single(x => x.UserID == model.UserID && x.ReportID == model.ReportID);

                targetRating.Rank = model.Rank;
            }
            else
            {
                var newRating = new Rating();

                newRating.RatingID = Guid.NewGuid();
                newRating.ReportID = model.ReportID;
                newRating.UserID = model.UserID;
                newRating.Rank = model.Rank;

                db.Ratings.AddObject(newRating);
            }

            db.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}
