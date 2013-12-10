using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.IO;

using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

using Chwinock.Common;
using Chwinock.Models;

namespace Chwinock.Controllers
{
    public class AdminController : Controller
    {
        //
        // GET: /Admin/

        public ActionResult Manage(String name)
        {
            return View();
        }

        [HttpPost]
        public ActionResult CreateUser(String userId, String name)
        {
            var db = new ChwinockEntities();

            if (!db.Users.Any(u => u.UserID == userId))
            {
                var newUser = new User();
                newUser.UserID = userId;
                newUser.Name = name;
                // User starts with normal name
                newUser.ChillName = name;

                db.Users.AddObject(newUser);

                db.SaveChanges();
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public ActionResult CreateReport()
        {
            ViewBag.Action = "CreateReport";

            return View();
        }

        [HttpPost]
        public ActionResult CreateReport(HttpPostedFileBase file, Report model, String PhotoDataChoice)
        {
            file = Request.Files[0];

            var photoId = Guid.NewGuid();
            var photoUrl = photoId.ToString() + "." + file.FileName.Split('.')[1];
            var reportId = Guid.NewGuid();

            /** Set up report in DB **/
            var db = new ChwinockEntities();

            model.ReportID = reportId;
            model.CreateDate = DateTime.UtcNow;
            model.Content = model.Content.Replace("\n", "<br />");

            // Set up photo
            model.Photo.Url = photoUrl;
            model.Photo.PhotoID = photoId;
            model.Photo.ReportID = reportId;

            // Get extra data
            if (PhotoDataChoice == "Metadata")
            {
                model = Common.Utilities.GetPhotoMetadata(model, file);
            }
            else
            {
                model = Common.Utilities.GetPhotoLocation(model);
            }

            db.Photos.AddObject(model.Photo);

            Common.Utilities.UploadPhoto(file, photoUrl);

            // Connect report to photo
            model.PhotoID = photoId;

            db.Reports.AddObject(model);

            //Save everything in DB
            db.SaveChanges();

            return RedirectToAction("Manage");
        }

        [HttpGet]
        public ActionResult EditReport(Guid reportId)
        {
            // Need to strip out the html from the content
            var targetReport = new ChwinockEntities()
                .Reports
                .Single(r => r.ReportID == reportId);

            targetReport.Content = targetReport.Content.Replace("<br />", "\n");

            ViewBag.Action = "EditReport";

            return View("CreateReport", targetReport);
        }

        [HttpPost]
        public ActionResult EditReport(Report model, String PhotoDataChoice)
        {
            var db = new ChwinockEntities();

            var targetReport = db.Reports.Single(r => r.ReportID == model.ReportID);

            // If a file was added, change the photos
            if (Request.Files[0].ContentLength > 0)
            {
                // Delete old photo blob
                //Common.Utilities.DeletePhotoBlob(targetReport.PhotoID);

                // Add new photo blob
                HttpPostedFileBase file = Request.Files[0];
                var photoUrl = targetReport.PhotoID.ToString() + "." + file.FileName.Split('.')[1];

                Common.Utilities.UploadPhoto(file, photoUrl);

                // Change the db entry, we change the URL in case the 
                // extension of the image has changed, but the actual
                // Guid remains the same
                targetReport.Photo.Url = photoUrl;

                // Get extra data
                if (PhotoDataChoice == "Metadata")
                {
                    targetReport = Common.Utilities.GetPhotoMetadata(model, file);
                }
                else
                {
                    targetReport = Common.Utilities.GetPhotoLocation(model);
                }

            }

            // Change other photo attributes
            targetReport.Photo.Title = model.Photo.Title;
            targetReport.Photo.Caption = model.Photo.Caption;
            targetReport.Photo.Location = model.Photo.Location;

            // Change report properties
            targetReport.Title = model.Title;
            targetReport.Author = model.Author;

            // Convert formatting
            model.Content = model.Content.Replace("\n", "<br />");
            targetReport.Content = model.Content;

            db.SaveChanges();

            return RedirectToAction("Manage");
        }

        public ActionResult DeleteReport(Guid reportId)
        {
            var db = new ChwinockEntities();

            db = Common.Utilities.DeleteReport(db, reportId);

            db.SaveChanges();

            return RedirectToAction("Manage");
        }

        [HttpGet]
        public ActionResult CreateSong()
        {
            ViewBag.Action = "CreateSong";

            return View(new CreateSongVM());
        }

        [HttpPost]
        public ActionResult CreateSong(CreateSongVM model, List<String> AssociatedReports)
        {
            var db = new ChwinockEntities();

            var newSong = new Song();
            newSong.SongID = Guid.NewGuid();
            newSong.Artist = model.Song.Artist;
            newSong.Name = model.Song.Name;
            newSong.Url = model.Song.Url;

            if (AssociatedReports != null)
            {
                foreach (var r in AssociatedReports)
                {
                    var id = Guid.Parse(r);
                    newSong.Reports.Add(db.Reports.Single(x => x.ReportID == id));
                }
            }

            db.Songs.AddObject(newSong);

            db.SaveChanges();

            return RedirectToAction("Manage");
        }

        [HttpGet]
        public ActionResult EditSong(Guid songId)
        {
            // Need to strip out the html from the content
            var targetSong = new ChwinockEntities()
                .Songs
                .Single(r => r.SongID == songId);

            ViewBag.Action = "EditSong";

            return View("CreateSong", new CreateSongVM(targetSong));
        }

        [HttpPost]
        public ActionResult EditSong(CreateSongVM model, List<String> AssociatedReports)
        {
            var db = new ChwinockEntities();

            var targetSong = db.Songs.Single(r => r.SongID == model.Song.SongID);

            targetSong.Artist = model.Song.Artist;
            targetSong.Name = model.Song.Name;
            targetSong.Url = model.Song.Url;

            // Remove associated reports
            List<Report> reportsToRemove = new List<Report>();
            foreach (var r in targetSong.Reports)
            {
                reportsToRemove.Add(r);
            }
            foreach (var r in reportsToRemove)
            {
                targetSong.Reports.Remove(r);
            }

            if (AssociatedReports != null)
            {
                // Add new ones
                foreach (var r in AssociatedReports)
                {
                    var id = Guid.Parse(r);
                    targetSong.Reports.Add(db.Reports.Single(x => x.ReportID == id));
                }
            }

            db.SaveChanges();

            return RedirectToAction("Manage");
        }

        public ActionResult DeleteSong(Guid songId)
        {
            var db = new ChwinockEntities();

            var songToDelete = db.Songs.Single(s => s.SongID == songId);

            // Delete report attachments
            var reportsToDelete = new List<Report>();
            foreach (var r in songToDelete.Reports)
            {
                reportsToDelete.Add(r);
            }
            foreach (var r in reportsToDelete)
            {
                songToDelete.Reports.Remove(r);
            }

            db.Songs.DeleteObject(songToDelete);

            db.SaveChanges();

            return RedirectToAction("Manage");
        }

        [HttpGet]
        public ActionResult CreateAlcohol()
        {
            ViewBag.Action = "CreateAlcohol";

            return View(new CreateAlcoholVM());
        }

        [HttpPost]
        public ActionResult CreateAlcohol(CreateAlcoholVM model, List<String> AssociatedReports)
        {
            var db = new ChwinockEntities();

            var newAlcohol = new Alcohol();
            newAlcohol.AlcoholID = Guid.NewGuid();
            newAlcohol.Name = model.Alcohol.Name;
            newAlcohol.Price = model.Alcohol.Price;
            newAlcohol.Percentage = model.Alcohol.Percentage;
            newAlcohol.Type = model.Alcohol.Type;
            newAlcohol.Thoughts = model.Alcohol.Thoughts;

            if (AssociatedReports != null)
            {
                foreach (var r in AssociatedReports)
                {
                    var id = Guid.Parse(r);
                    newAlcohol.Reports.Add(db.Reports.Single(x => x.ReportID == id));
                }
            }

            db.Alcohols.AddObject(newAlcohol);

            db.SaveChanges();

            return RedirectToAction("Manage");
        }

        [HttpGet]
        public ActionResult EditAlcohol(Guid alcoholId)
        {
            // Need to strip out the html from the content
            var targetAlcohol = new ChwinockEntities()
                .Alcohols
                .Single(r => r.AlcoholID == alcoholId);


            targetAlcohol.Thoughts = targetAlcohol.Thoughts.Replace("<br />", "\n");

            ViewBag.Action = "EditAlcohol";

            return View("CreateAlcohol", new CreateAlcoholVM(targetAlcohol));
        }

        [HttpPost]
        public ActionResult EditAlcohol(CreateAlcoholVM model, List<String> AssociatedReports)
        {
            var db = new ChwinockEntities();

            var targetAlcohol = db.Alcohols.Single(r => r.AlcoholID == model.Alcohol.AlcoholID);

            targetAlcohol.Name = model.Alcohol.Name;
            targetAlcohol.Type = model.Alcohol.Type;
            targetAlcohol.Price = model.Alcohol.Price;
            targetAlcohol.Percentage = model.Alcohol.Percentage;
            targetAlcohol.Thoughts = model.Alcohol.Thoughts;


            // Remove associated reports
            List<Report> reportsToRemove = new List<Report>();
            foreach (var r in targetAlcohol.Reports)
            {
                reportsToRemove.Add(r);
            }
            foreach (var r in reportsToRemove)
            {
                targetAlcohol.Reports.Remove(r);
            }
            if (AssociatedReports != null)
            {
                // Add new ones
                foreach (var r in AssociatedReports)
                {
                    var id = Guid.Parse(r);
                    targetAlcohol.Reports.Add(db.Reports.Single(x => x.ReportID == id));
                }
            }

            db.SaveChanges();

            return RedirectToAction("Manage");
        }

        public ActionResult DeleteAlcohol(Guid alcoholId)
        {
            var db = new ChwinockEntities();

            var alcoholToDelete = db.Alcohols.Single(s => s.AlcoholID == alcoholId);

            // Delete report attachments
            var reportsToDelete = new List<Report>();
            foreach (var r in alcoholToDelete.Reports)
            {
                reportsToDelete.Add(r);
            }
            foreach (var r in reportsToDelete)
            {
                alcoholToDelete.Reports.Remove(r);
            }

            db.Alcohols.DeleteObject(alcoholToDelete);

            db.SaveChanges();

            return RedirectToAction("Manage");
        }

        public ActionResult DeleteUser(String userId)
        {
            var db = new ChwinockEntities();

            var targetUser = db.Users.Single(u => u.UserID == userId);

            List<Comment> commentsToDelete = new List<Comment>();
            foreach (var c in targetUser.Comments)
            {
                commentsToDelete.Add(c);
            }
            foreach (var c in commentsToDelete)
            {
                db.Comments.DeleteObject(c);
            }

            List<Rating> ratingsToDelete = new List<Rating>();
            foreach (var c in targetUser.Ratings)
            {
                ratingsToDelete.Add(c);
            }
            foreach (var c in ratingsToDelete)
            {
                db.Ratings.DeleteObject(c);
            }

            db.Users.DeleteObject(targetUser);

            db.SaveChanges();

            return RedirectToAction("Manage");
        }

        [HttpGet]
        public PartialViewResult ManageReports()
        {
            return PartialView("_ManageReports", new ChwinockEntities().Reports.OrderByDescending(x => x.CreateDate));
        }

        [HttpGet]
        public PartialViewResult ManageSongs()
        {
            return PartialView("_ManageSongs", new ChwinockEntities().Songs);
        }

        [HttpGet]
        public PartialViewResult ManageAlcohol()
        {
            return PartialView("_ManageAlcohol", new ChwinockEntities().Alcohols);
        }

        public PartialViewResult ManageUsers()
        {
            return PartialView("_ManageUsers", new ChwinockEntities().Users);
        }
    }
}
