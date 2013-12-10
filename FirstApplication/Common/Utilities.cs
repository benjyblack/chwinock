using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;

using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

using Chwinock.Models;

using System.Windows.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using System.Net;

namespace Chwinock.Common
{
    public static class Utilities
    {
        public static String GenerateName(String fullname, StreamReader reader)
        {
            string line;
            string firstName = fullname.Split(' ')[0];

            List<String> candidates = new List<String>();
            while ((line = reader.ReadLine()) != null)
            {
                if (line.First().ToString() == firstName.First().ToString().ToLower())
                {
                    candidates.Add(line);
                }
            }

            Random rand = new Random((int)DateTime.Now.Ticks);

            String winner = candidates.ElementAt(rand.Next(0, candidates.Count()));

            String winnerFormatted = "";

            foreach (var e in winner.Split(' '))
            {
                winnerFormatted += char.ToUpper(e[0]) + e.Substring(1) + " ";
            }

            return winnerFormatted + firstName;
        }

        public static bool UploadPhoto(HttpPostedFileBase file, String photoUrl)
        {
            try
            {
                /** Upload file to Azure **/
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                    ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString);

                // Create the blob client.
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                // Retrieve reference to a previously created container.
                CloudBlobContainer container = blobClient.GetContainerReference("photos");

                // Retrieve reference to a blob named "myblob".
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(photoUrl);

                // Create or overwrite the "myblob" blob with contents from a local file.
                file.InputStream.Position = 0;
                blockBlob.UploadFromStream(file.InputStream);

                return true;
            }
            catch
            {
            }

            return false;
        }

        public static Report GetPhotoMetadata(Report model, HttpPostedFileBase file)
        {
            dynamic metadata = MetaDataFor(file);
            model.Photo.CreateDate = metadata.date;
            model.Photo.Lat = metadata.lat;
            model.Photo.Lng = metadata.lng;

            return model;
        }

        public static Report GetPhotoLocation(Report model)
        {
            dynamic loc_data = Common.Utilities.GetLatLngFor(model.Photo.Location);
            model.Photo.CreateDate = DateTime.UtcNow;
            model.Photo.Lat = loc_data.lat;
            model.Photo.Lng = loc_data.lng;

            return model;
        }

        public static ChwinockEntities DeleteReport(ChwinockEntities db, Guid ReportID)
        {
            var targetReport = db.Reports.Single(r => r.ReportID == ReportID);

            // Delete ratings
            var ratingsToDelete = new List<Rating>();
            foreach (var r in targetReport.Ratings)
            {
                ratingsToDelete.Add(r);
            }
            foreach (var r in ratingsToDelete)
            {
                db.Ratings.DeleteObject(r);
            }

            // Delete comments
            var commentsToDelete = new List<Comment>();
            foreach (var c in targetReport.Comments)
            {
                commentsToDelete.Add(c);
            }
            foreach (var c in commentsToDelete)
            {
                db.Comments.DeleteObject(c);
            }

            // Delete photos
            db = DeletePhoto(db, targetReport.PhotoID);

            // Delete music attachments
            var songsToDelete = new List<Song>();
            foreach (var r in targetReport.Songs)
            {
                songsToDelete.Add(r);
            }
            foreach (var r in songsToDelete)
            {
                targetReport.Songs.Remove(r);
            }

            // Delete alcohol attachments
            var alcoholToDelete = new List<Alcohol>();
            foreach (var r in targetReport.Alcohols)
            {
                alcoholToDelete.Add(r);
            }
            foreach (var r in alcoholToDelete)
            {
                targetReport.Alcohols.Remove(r);
            }

            // Delete report
            db.DeleteObject(targetReport);

            return db;
        }

        public static ChwinockEntities DeletePhoto(ChwinockEntities db, Guid photoId)
        {
            var targetPhoto = db.Photos.Single(p => p.PhotoID == photoId);

            // Delete from azure
            if (DeletePhotoBlob(photoId))
            {
                // Delete from db
                db.Photos.DeleteObject(targetPhoto);
            }
            
            return db;
        }

        public static bool DeletePhotoBlob(Guid photoId)
        {
            try
            {
                // Delete from Azure
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                    ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString);

                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                // Get a reference to the container.
                CloudBlobContainer container = blobClient.GetContainerReference("photos");

                // Indicate that any snapshots should be deleted.
                BlobRequestOptions options = new BlobRequestOptions();
                options.DeleteSnapshotsOption = DeleteSnapshotsOption.IncludeSnapshots;

                // Specify a flat blob listing, so that only CloudBlob objects will be returned.
                // The Delete method exists only on CloudBlob, not on IListBlobItem.
                options.UseFlatBlobListing = true;

                var blob = container.GetBlobReference(photoId.ToString());

                blob.Delete(options);

                return true;
            }
            catch
            {
            }

            return false;
        }

        public static Object MetaDataFor(HttpPostedFileBase file)
        {
            double GeoLat = 0;
            double GeoLong = 0;
            DateTime CreateDate = DateTime.UtcNow;

            try
            {
                BitmapSource img = BitmapFrame.Create(file.InputStream);
                
                BitmapMetadata bitmapMetadata = (BitmapMetadata)img.Metadata;

                // Get Date
                try
                {
                    CreateDate = DateTime.Parse(bitmapMetadata.DateTaken);
                }
                catch
                {
                    CreateDate = DateTime.UtcNow;
                }
                // Get LatLng
                byte[] Version = (byte[])bitmapMetadata.GetQuery(@"/app1/ifd/gps/");
                if (Version != null)
                {
                    ulong[] GeoLatInfo = (ulong[])bitmapMetadata.GetQuery(@"/app1/ifd/gps/subifd:{ulong=2}");
                    string GeoLatDirection = (string)bitmapMetadata.GetQuery(@"/app1/ifd/gps/subifd:{char=1}");
                    ulong[] GeoLongInfo = (ulong[])bitmapMetadata.GetQuery(@"/app1/ifd/gps/subifd:{ulong=4}");
                    string GeoLongDirection = (string)bitmapMetadata.GetQuery(@"/app1/ifd/gps/subifd:{char=3}");

                    if (GeoLatInfo != null && GeoLatDirection != null)
                    {
                        GeoLat = ConvertCoordinate(GeoLatInfo);
                        if (GeoLatDirection == "S")
                            GeoLat = -GeoLat;
                    }

                    if (GeoLongInfo != null && GeoLongDirection != null)
                    {
                        GeoLong = ConvertCoordinate(GeoLongInfo);
                        if (GeoLongDirection == "W")
                            GeoLong = -GeoLong;
                    }
                }
            }
            catch { }

            return new { lat = GeoLat, lng = GeoLong, date = CreateDate };
        }

        private static double ConvertCoordinate(ulong[] coordinates)
        {
            double degrees;
            double minutes;
            double seconds;

            degrees = splitLongAndDivide(coordinates[0]);
            minutes = splitLongAndDivide(coordinates[1]);
            seconds = splitLongAndDivide(coordinates[2]);

            double coordinate = degrees + ((double)minutes / 60.0) + (seconds / 3600);

            return coordinate;
        }

        private static double splitLongAndDivide(ulong number)
        {
            byte[] bytes = BitConverter.GetBytes(number);
            int int1 = BitConverter.ToInt32(bytes, 0);
            int int2 = BitConverter.ToInt32(bytes, 4);
            return ((double)int1 / (double)int2);
        }

        public static Object GetLatLngFor(String location)
        {
            string url = "http://maps.googleapis.com/maps/api/geocode/json?sensor=true&address=";

            dynamic googleResults = new Uri(url + location).GetDynamicJsonObject();

            return new { lat = googleResults.results[0].geometry.location.lat, lng = googleResults.results[0].geometry.location.lng };
        }
    }
}