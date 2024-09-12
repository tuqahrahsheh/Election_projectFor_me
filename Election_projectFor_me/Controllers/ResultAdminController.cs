﻿using Election_projectFor_me.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Election_projectFor_me.Controllers
{
    public class ResultAdminController : Controller
    {

        private ELECTION_PROJECTEntities db = new ELECTION_PROJECTEntities();
        // GET: Result
        public ActionResult ListAdmin()
        {

            int totalVotes = db.USERS
           .Sum(c => (c.PartyElections.HasValue && c.PartyElections.Value ? 1 : 0) + (c.whitePaperPartyElections.HasValue && c.whitePaperPartyElections.Value ? 1 : 0));
            double percentage = 2.5 / 100;
            double basic = percentage * totalVotes;



            var listingsAboveBasic = db.GeneralListings
               .Where(gl => gl.NumberOfVotes.HasValue && gl.NumberOfVotes.Value > basic)
               .ToList();
            Session["listingsAboveBasic"] = listingsAboveBasic;
            foreach (var l in db.GeneralListings)
            {

                l.Sucess = false;

            }
            foreach (var listing in listingsAboveBasic)
            {
                listing.Sucess = true;
            }

            db.SaveChanges();





            return View(db.GeneralListings.ToList());


        }
        public ActionResult TableOfListAdmin(int? id)
        {
            var listingsAboveBasic = Session["listingsAboveBasic"] as List<GeneralListing>;

            double totalVotes = listingsAboveBasic
              .Where(gl => gl.NumberOfVotes.HasValue)
              .Sum(gl => gl.NumberOfVotes.Value);


            var totalVotesForGeneral = db.GeneralListings
             .Where(g => g.GeneralListingID == id)
             .FirstOrDefault().NumberOfVotes;

            double numberOfSeats = totalVotesForGeneral.HasValue ? totalVotesForGeneral.Value / totalVotes : 0;


            int intPart = (int)numberOfSeats;
            double fractionalPart = numberOfSeats - intPart;

            if (fractionalPart == 0.5)
            {

                numberOfSeats = intPart;
            }
            else
            {

                numberOfSeats = Math.Round(numberOfSeats);
            }

            var successGeneralCandidates = db.GeneralListCandidates
               .Where(c => c.GeneralListingID == id)
               .Take((int)numberOfSeats)
               .ToList();

            foreach (var candidate in successGeneralCandidates)
            {

                candidate.Sucess = true;

            }
            db.SaveChanges();




            var list = db.GeneralListCandidates
     .Where(l => l.GeneralListingID == id)
     .ToList();


            return View(list);
        }


        public ActionResult ElectionsAreasAdmin()
        {

            var electionService = new ElectionService();

            // Step 1: Fetch the data from the database
            var electionsAreasData = db.LocalLists
                .GroupBy(l => l.ElectionArea)
                .Select(group => new
                {
                    ElectionArea = group.Key,
                    Governorate = group.FirstOrDefault().Governorate,
                    NumberOfVotes = (int)group.Sum(l => l.NumberOfVotes),
                    WhitePaperLocalElectionsCount = db.USERS
                        .Where(u => u.ElectionArea == group.Key && u.whitePaperLocalElections == true)
                        .Count()
                })
                .ToList();

            // Step 2: Use the ElectionService to calculate NumberOfSeats
            var electionsAreas = electionsAreasData.Select(data => new ElectionsAreas
            {
                ElectionArea = data.ElectionArea,
                Governorate = data.Governorate,
                NumberOfVotes = data.NumberOfVotes,
                WhitePaperLocalElectionsCount = data.WhitePaperLocalElectionsCount,
                NumberOfSeats = electionService.GetNumberOfSeats(data.ElectionArea),
                TheRatioOfBase = 0.07 * data.NumberOfVotes,
            }).ToList();

            Session["ElectionsAreas"] = electionsAreas;





            return View(electionsAreas);
        }


        public ActionResult ListOfCandidatesAdmin(string id)
        {

            var electionService = new ElectionService();
            var electionsAreas = Session["ElectionsAreas"] as List<ElectionsAreas>;



            if (electionsAreas == null)
            {
                return RedirectToAction("ElectionsAreas");
            }


            var specificArea = electionsAreas
                .FirstOrDefault(l => l.ElectionArea == id);
            double TotalOfVotesArea = specificArea.NumberOfVotes;
            double Base = specificArea.TheRatioOfBase;




            var localListsWithSucessVotes = db.LocalLists

.Where(l => l.ElectionArea == id)
.Where(l => l.NumberOfVotes > Base)
.OrderByDescending(l => l.NumberOfVotes)
.ToList();



            foreach (var l in localListsWithSucessVotes)
            {
                l.Sucess = true;
            }
            db.SaveChanges();

            Session["LocalListsWithSucessVotes"] = localListsWithSucessVotes;





            var localList = db.LocalLists
                    .Where(l => l.ElectionArea == id)
                    .ToList();
            int totalVotesInFilteredLists = localListsWithSucessVotes?.Sum(l => l.NumberOfVotes ?? 0) ?? 0;


            int electoralSeats = specificArea.NumberOfSeats;


            var totalVotesForList = db.LocalLists.Where(x => x.ElectionArea == id)
            .GroupBy(l => new { l.ID, l.ListName })
            .Select(group => new
            {
                ID = group.Key.ID,
                ListName = group.Key.ListName,
                NumberOfVotes = group.Sum(l => l.NumberOfVotes ?? 0)
            })
            .ToList();


            var locals = totalVotesForList.Select(item => new Local
            {
                ID = item.ID,
                ListName = item.ListName,
                NumberOfVotes = item.NumberOfVotes,
                Ratio = electionService.CalculateRatio(item.NumberOfVotes, (double)totalVotesInFilteredLists, electoralSeats)
            }).ToList();

            var successCandidates = new List<LocalListCandidate>();

            foreach (var local in locals)
            {
                var topCandidates = db.LocalListCandidates
                    .Where(c => c.listname == local.ListName && c.typeofCandidates == "تنافس")
                    .OrderByDescending(c => c.NumberOfVotesCandidate)
                    .Take((int)local.Ratio)
                    .ToList();

                successCandidates.AddRange(topCandidates);




                foreach (var item in topCandidates)
                {
                    item.Sucess = true;
                }
                var successfulListNames = localListsWithSucessVotes
   .Select(l => l.ListName)
   .ToList();
                var topMasehe = db.LocalListCandidates
                  .Where(c => successfulListNames.Contains(c.listname) && c.typeofCandidates == "مسيحي")
                  .OrderByDescending(c => c.NumberOfVotesCandidate)
                  .Take(1)
                  .ToList();

                foreach (var item in topMasehe)
                {
                    item.Sucess = true;
                }

                var topQota = db.LocalListCandidates
        .Where(c => successfulListNames.Contains(c.listname) && c.typeofCandidates == "كوتا")
        .OrderByDescending(c => c.NumberOfVotesCandidate)
        .Take(1)
        .ToList();
                foreach (var item in topQota)
                {
                    item.Sucess = true;
                }
            }

            db.SaveChanges();




            var groupedCandidates = db.LocalListCandidates
                .Where(c => c.ElectionArea == id)
                .GroupBy(c => c.listname)
                .Select(group => new CandidateGroup
                {
                    ListName = group.Key,
                    Candidates = group.ToList()
                })
                .ToList();

            ViewBag.id = id;
            Session["groupedCandidates"] = groupedCandidates;


            return View(groupedCandidates);





        }















    }
}
