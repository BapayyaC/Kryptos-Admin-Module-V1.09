using Kryptos.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Web.UI;
using Newtonsoft.Json;
using System.Web.Configuration;
using System.Data.SqlClient;
using CommonHelper;
using System.Globalization;


namespace Kryptos.Controllers
{
    public class UserDatatablesController : Controller
    {
        //
        // GET: /UserDatatables/

        kryptoEntities1 _context = new kryptoEntities1();

        public ActionResult List()
        {
            string facilityval = this.Request.QueryString["Fac"];
            string value = this.Request.QueryString["Value"];

            if (facilityval != null)
            {
                TempData["fac"] = facilityval;
            }

            if (value != null)
            {
                TempData["Org"] = value;
            }

            ViewData["current view"] = "Users List";
            if (LoginController.ActiveUser != null)
            {
                ViewData["CURRENTUSER"] = LoginController.ActiveUser;
                if (LoginController.ActiveUser.IsOrganisationAdmin && LoginController.ActiveUser.IsSuperAdmin == false)
                {
                    var Organizations = (from OrA in _context.OrganizationAdmins
                                         join Org in _context.Organisations on OrA.OrganisationId equals Org.OrganisationId
                                         where OrA.USERID == LoginController.ActiveUser.USERID
                                         select Org.Name).ToList();
                    //TempData["OrgAdmin"] = Organizations;
                    Session["OrgAdmin"] = Organizations;
                }
                else if (LoginController.ActiveUser.IsFacilityAdmin && LoginController.ActiveUser.IsOrganisationAdmin == false)
                {
                    var Facilities = (from Fa in _context.FacilityAdmins
                                      join Fm in _context.FacilityMasters on Fa.FacilityMasterId equals Fm.FacilityMasterId
                                      where Fa.USERID == LoginController.ActiveUser.USERID
                                      select Fm.FacilityMasterName).ToList();

                   // TempData["FacAdmin"] = Facilities;
                    Session["FacAdmin"] = Facilities;
                }
            }
            else
            {
                TempData["errormsg"] = "Session was expired.Please Login Again";
               // TempData["errormsg"] = "Please Login and Proceed!";
                return RedirectToAction("Login", "Login");
            }
            return View();
        }
        [ActionName("OrgList")]
        public ActionResult List(string Value)
        {
            ViewData["Org"] = Value;
            return View();
        }
        private List<UserLoginInformation> GetMatchingUsersForUserRole(UserLoginInformation currentUser)
        {
            
            List<UserLoginInformation> users = null;
            if (currentUser.IsSuperAdmin)
            {
                users = _context.UserLoginInformations.Where(x => x.Status == 1).ToList();
            }
            else if (currentUser.IsOrganisationAdmin)
            {
                var orgadminslist = _context.OrganizationAdmins.Where(x => x.USERID == currentUser.USERID).ToList();
                var selectedorg = orgadminslist.Select(x => x.OrganisationId).ToList();

                var selections = new int[selectedorg.Count()];
                for (var i = 0; i < selectedorg.Count(); i++)
                {
                    selections[i] = selectedorg[i];
                }
         

                users = (from user in _context.UserLoginInformations
                         join facility in _context.FacilityMasters on user.FacilityId equals facility.FacilityMasterId
                         join organisation in _context.Organisations on facility.OrganisationId equals
                             organisation.OrganisationId
                        // where facility.OrganisationId == currentUser.Facility.OrganisationId && user.Status == 1
                         where selectedorg.Any(i => organisation.OrganisationId.Equals(i) && user.Status == 1)
                         select user).ToList();
            }
            else if (currentUser.IsFacilityAdmin)
            {
                var faclist = _context.FacilityAdmins.Where(x => x.USERID == currentUser.USERID).ToList();
                var selectedfacadmins = faclist.Select(x => x.FacilityMasterId).ToList();

                var selections = new int[selectedfacadmins.Count()];

                for (var i = 0; i < selectedfacadmins.Count(); i++)
                {
                    selections[i] = selectedfacadmins[i];
                }
         

                users = (from user in _context.UserLoginInformations
                         join facility in _context.FacilityMasters on user.FacilityId equals facility.FacilityMasterId
                        // where facility.FacilityMasterId == currentUser.Facility.FacilityMasterId && user.Status == 1
                        where selectedfacadmins.Any(i=>facility.FacilityMasterId.Equals(i)&&user.Status==1)
                         select user).ToList();
            }
            return users;
        }

        public ActionResult UserInfoList()
        {
            return Json(new { aaData = GetMatchingUsersForUserRole(LoginController.ActiveUser) },
                JsonRequestBehavior.AllowGet);
        }

        public JsonResult getMatchingUser(int selecteduser)
        {
            var userlogininfo =
                _context.UserLoginInformations.Single(x => x.USERID.Equals(selecteduser) && x.Status == 1);
            userlogininfo.OtherFacilityIds = userlogininfo.GetOtherFacilityIds();
            return Json(userlogininfo, JsonRequestBehavior.AllowGet);
        }

        public UserLoginInformation Updateobject(int id, UserLoginInformation filled)
        {
            var obj = _context.UserLoginInformations.Find(id);
            var props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                var currentprop = prop.GetValue(filled);
                if (currentprop is Int32)
                {
                    var currentint = (int)currentprop;
                    if (currentint == 0)
                    {
                        prop.SetValue(filled, prop.GetValue(obj), null);
                    }
                }
                else if (currentprop is Int16)
                {
                    var currentInt16 = (Int16)currentprop;
                    if (currentInt16 == 0)
                    {
                        prop.SetValue(filled, prop.GetValue(obj), null);
                    }
                }
                else if (currentprop is Byte)
                {
                    var currentByte = (Byte)currentprop;
                    if (currentByte == 0)
                    {
                        prop.SetValue(filled, prop.GetValue(obj), null);
                    }
                }
                else if (currentprop is Boolean)
                {
                    var currentBoolean = (Boolean)currentprop;
                    if (currentBoolean == (Boolean)prop.GetValue(obj))
                    {
                        prop.SetValue(filled, prop.GetValue(obj), null);
                    }
                }
                else if (currentprop is String)
                {
                    var currentstring = (string)currentprop;
                    if (currentstring.Length == 0)
                    {
                        prop.SetValue(filled, prop.GetValue(obj), null);
                    }
                }
                else if (currentprop is DateTime)
                {
                    var currentDateTime = (DateTime)currentprop;
                    if (currentDateTime == new DateTime())
                    {
                        prop.SetValue(filled, prop.GetValue(obj), null);
                    }
                }
                else
                {
                    if (currentprop == null)
                    {
                        prop.SetValue(filled, prop.GetValue(obj), null);
                    }
                }
            }
            return filled;
        }

        private List<Organisation> GetMatchingOrganisationsForUserRole(UserLoginInformation currentUser)
        {
            List<Organisation> orgs = null;
            if (currentUser.IsSuperAdmin)
            {
                orgs = _context.Organisations.Where(x => x.Status == 1).ToList();      
            }
            else if (currentUser.IsOrganisationAdmin)
            {
                var orglist = currentUser.GetUserOrganizationList();
                var selectedorgs = orglist.Select(x => x.OrganisationId).ToList();
                var selections = new int[selectedorgs.Count()];
                for (var i = 0; i < selectedorgs.Count(); i++)
                {
                    selections[i] = selectedorgs[i];
                }
                orgs = (from org in _context.Organisations
                        join orgadmin in _context.OrganizationAdmins on org.OrganisationId equals orgadmin.OrganisationId
                        where selectedorgs.Any(i => orgadmin.OrganisationId.Equals(i) && org.Status == 1 && orgadmin.USERID.Equals(currentUser.USERID))
                        select org).ToList();
            }
            else if(currentUser.IsFacilityAdmin)
            {
              string str=  currentUser.Organisations;

              var selectedorgs = str.Split(',').ToArray();
              var selections = new int[selectedorgs.Count()];
              for (var i = 0; i < selectedorgs.Count(); i++)
              {
                  selections[i] = int.Parse(selectedorgs[i]);
              }
              orgs = (from org in _context.Organisations
                      where selections.Any(i => org.OrganisationId.Equals(i) && org.Status == 1)
                      select org).ToList();

            }
            return orgs;
        }

        public ActionResult GetAllOrganisations()
        {
            return Json(GetMatchingOrganisationsForUserRole(LoginController.ActiveUser), JsonRequestBehavior.AllowGet);
           // return Json(_context.Organisations.Where(x => x.Status == 1).ToList(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetMatchingFacilitiesForSelectedOrganisationAndPrimaryFacilty(string selectedOrgs,
            string selectedPrimary)
        {
            if (selectedOrgs == "---") return null;
            if (selectedPrimary == "---" || selectedPrimary == "0") return null;
            var selectionStrings = selectedOrgs.Split(',');
            var selections = new int[selectionStrings.Count()];
            for (var i = 0; i < selectionStrings.Count(); i++)
            {
                selections[i] = int.Parse(selectionStrings[i]);
            }

            var facilityMasters = (from facilityMaster in _context.FacilityMasters.AsQueryable()
                                   where selections.Any(i => facilityMaster.OrganisationId.Equals(i))
                                   select facilityMaster).OrderBy(x => x.OrganisationId).ToList();

            var finalfacilityMasters = facilityMasters;

            var templist =
                facilityMasters.Where(facility => facility.FacilityMasterId == int.Parse(selectedPrimary)).ToList();

            if (templist.Count > 0)
            {
                foreach (var facility in templist)
                {
                    finalfacilityMasters.Remove(@facility);
                }
            }
            return Json(finalfacilityMasters, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetAllTitles()
        {
            //var titles = _context.Titles.Select(new { x.Name, x.TitleID });
            return Json(_context.Titles.ToList(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetAllCountries()
        {
            return Json(_context.Countries.ToList(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetStatesBasedOnCountry(int selectedCountry)
        {
            List<State> stateslist = _context.States.Where(x => x.CountryId == selectedCountry).ToList();
            return Json(stateslist, JsonRequestBehavior.AllowGet);
        }

        public ActionResult UpdateUserStatus(int currentRecord, bool currentStatus)
        {
            var info = _context.UserLoginInformations.Single(x => x.USERID == currentRecord && x.Status == 1);
            var loggedinUser = LoginController.ActiveUser;

            if (info.IsActive != currentStatus)
            {
                info.IsActive = currentStatus;
                info.UserIsActive = currentStatus;
                try
                {
                    using (var transactionScope = new TransactionScope())
                    {
                        try
                        {
                            using (kryptoEntities1 db = new kryptoEntities1()) // Context object
                            {
                                if (info.IsActive)
                                {
                                    info.ActivatedDate = DateTime.Now;
                                }
                                else
                                {
                                    info.DeactivatedDate = DateTime.Now;
                                }
                                db.Entry(info).State = EntityState.Modified;
                                db.SaveChanges();
                                var useracive = new UserActivate
                                {
                                    CreatedById = loggedinUser.USERID.ToString(),
                                    Date = DateTime.Now,
                                    USERID = info.USERID,
                                    IsActive = info.IsActive
                                };
                                db.UserActivates.Add(useracive);
                                db.SaveChanges();
                            }
                            transactionScope.Complete();
                        }
                        catch (Exception Wx)
                        {
                            return Json("Something went Wrong!", JsonRequestBehavior.AllowGet);
                        }
                    }
                    if (info.USERID > 0 && info.IsActive)
                    {
                        OtpSent(info, loggedinUser.USERID);
                    }
                    else if (info.IsActive == false)
                        RemoveUser(info.USERID);
                }
                catch (Exception Ex)
                {
                    return Json("Something went Wrong!", JsonRequestBehavior.AllowGet);
                }
                return Json("Sucessfully Updated the Status", JsonRequestBehavior.AllowGet);
            }
            return Json("No Changes to Update", JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetCitiesBasedOnState(int selectedCity)
        {
            List<City> citieslist = _context.Cities.Where(x => x.State == selectedCity).ToList();

            return Json(citieslist, JsonRequestBehavior.AllowGet);
        }

        private List<FacilityMaster> GetMatchingFacilitiesForUserRole(UserLoginInformation currentUser,
            int[] pSelections)
        {
            List<FacilityMaster> facilityMasters = null;
            if (currentUser.IsSuperAdmin)
            {
                facilityMasters = (from facilityMaster in _context.FacilityMasters
                                   where
                                       pSelections.Any(i => facilityMaster.OrganisationId.Equals(i) && facilityMaster.Status.Equals(1))
                                   select facilityMaster).OrderBy(x => x.OrganisationId).ToList();
            }
            else if (currentUser.IsOrganisationAdmin)
            {
                facilityMasters = (from facilityMaster in _context.FacilityMasters
                                   where
                                       pSelections.Any(
                                           i =>
                                               facilityMaster.OrganisationId.Equals(i) &&
                                               facilityMaster.Status.Equals(1))
                                   select facilityMaster).OrderBy(x => x.OrganisationId).ToList();
            }
            else if (currentUser.IsFacilityAdmin)
            {

                var allfacilities = (from facilityMaster in _context.FacilityMasters
                                     where
                                         pSelections.Any(i => facilityMaster.OrganisationId.Equals(i) && facilityMaster.Status.Equals(1))
                                     select facilityMaster).OrderBy(x => x.OrganisationId).ToList();
                var facadminfacilities = (from facadmin in _context.FacilityAdmins
                                          where facadmin.USERID == currentUser.USERID
                                          select facadmin).ToList();

                facilityMasters = (from fac in allfacilities
                                   join facadmin in facadminfacilities
                                   on fac.FacilityMasterId equals facadmin.FacilityMasterId
                                   select fac).OrderBy(x => x.OrganisationId).ToList();
               
            }
            return facilityMasters;
        }

        public ActionResult GetMatchingFacilityMasters(string selectedOrgs)
        {
            if (selectedOrgs == "---") return null;
            var selectionStrings = selectedOrgs.Split(',');
            var selections = new int[selectionStrings.Count()];
            for (var i = 0; i < selectionStrings.Count(); i++)
            {
                selections[i] = int.Parse(selectionStrings[i]);
            }
            var facilityMasters = GetMatchingFacilitiesForUserRole(LoginController.ActiveUser, selections);
            return Json(facilityMasters, JsonRequestBehavior.AllowGet);
        }

        private bool Hasitems(List<UserFacility> list1, List<UserFacility> list2)
        {
            var firstNotSecond = list1.Except(list2).ToList();
            var secondNotFirst = list2.Except(list1).ToList();
            return !firstNotSecond.Any() && !secondNotFirst.Any();
        }

        public static List<int> Union(List<int> firstList, List<int> secondList) 
        {
            if (firstList == null)
            {
                return secondList;
            }
            return secondList != null ? firstList.Union(secondList).ToList() : firstList;
        }

        public static List<int> Intersection(List<int> firstList, List<int> secondList)
        {
            if (firstList == null)
            {
                return null;
            }
            return secondList != null ? firstList.Intersect(secondList).ToList() : null;
        }

        public static List<int> ExcludedLeft(List<int> firstList, List<int> secondList)
        {
            return secondList != null ? Union(firstList, secondList).Except(secondList).ToList() : firstList;
        }

        public static List<int> ExcludedRight(List<int> firstList, List<int> secondList)
        {
            return firstList != null ? Union(firstList, secondList).Except(firstList).ToList() : secondList;
        }

        public ActionResult SubResults(String selections)
        {
            var responseNodes = JsonConvert.DeserializeObject<List<MyNode>>(selections);

            var resultFacilities = new List<MyFacility>();

            var resultOrgs = new List<MyOrganisation>();

            foreach (var @node in responseNodes)
            {
                var facility = new MyFacility
                {
                    Name = @node.text,
                    Value = @node.value,
                    ParentOrganisationId = @node.parent
                };

                var organisation = ChatGroupController.GetMatchingOrganisation(resultOrgs,
                    facility.GetParentOrganisation());

                if (organisation == null)
                {
                    resultOrgs.Add(facility.GetParentOrganisation());
                }
                resultFacilities.Add(facility);
            }

            foreach (var @myFacility in resultFacilities)
            {
                foreach (var @organisation in resultOrgs)
                {
                    if (ChatGroupController.GetMatchingFacilty(@organisation.TempFacilities, @myFacility) == null &&
                        ChatGroupController.GetMatchingFacilty(@organisation.GetAllMatchingFacilities(), @myFacility) !=
                        null)
                    {
                        @organisation.TempFacilities.Add(@myFacility);
                    }
                }
            }

            var nodes = new List<MyNode>();
            foreach (var @org in resultOrgs)
            {
                var orgNode = new MyNode
                {
                    text = org.Name,
                    value = org.Value,
                    icon = "glyphicon glyphicon-home",
                    backColor = "#ffffff",
                    color = "#428bca",
                    nodetype = MyNodeType.Organisation
                };
                var facilities = @org.TempFacilities;
                if (facilities != null && facilities.Count > 0)
                {
                    orgNode.nodes = new List<MyNode>();
                    foreach (var @fac in facilities)
                    {
                        var facNode = new MyNode
                        {
                            text = fac.Name,
                            value = fac.Value,
                            icon = "glyphicon glyphicon-th-list",
                            backColor = "#ffffff",
                            color = "#66512c",
                            parent = org.Value,
                            nodetype = MyNodeType.Facility
                        };
                        orgNode.nodes.Add(facNode);
                    }
                }
                nodes.Add(orgNode);
            }
            return Json(nodes, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Result(string selectedOrgs, string selectedPrimary, int currentrecord, bool selectExisting)
        {
            if (selectedOrgs == "---") return null;
            if (selectedPrimary == "---" || selectedPrimary == "0") return null;
            var selectionStrings = selectedOrgs.Split(',');
            var selections = new int[selectionStrings.Count()];
            for (var i = 0; i < selectionStrings.Count(); i++)
            {
                selections[i] = int.Parse(selectionStrings[i]);
            }

            var selectedPrimaryAsInt = int.Parse(selectedPrimary);

            var organisations = (from orgs in _context.Organisations.AsQueryable()
                                 where selections.Any(i => orgs.OrganisationId.Equals(i))
                                 select orgs).ToList();

            var myOrganisations =
                organisations.Select(
                    organisation => new MyOrganisation { Name = organisation.Name, Value = organisation.OrganisationId })
                    .ToList();

            List<int> participantsIds = null;
            if (currentrecord != 0)
            {
                var currentUser = _context.UserLoginInformations.Find(currentrecord);
                participantsIds = currentUser.GetFacilityIdsInUserFacilityList();
            }
            var nodes = new List<MyNode>();

            foreach (var @org in myOrganisations)
            {
                var myFacilities = @org.GetAllMatchingFacilities();
                if (myFacilities.Count() == 1 && selectionStrings.Count() == 1)
                {
                    nodes = null;
                }
                else
                {
                    MyUser user = new MyUser();
                    user.ParentFacilityId = selectedPrimaryAsInt;
                    MyFacility facility = user.GetParentFacility();
                    if ((facility.ParentOrganisationId == @org.Value && myFacilities.Count() != 1) || (facility.ParentOrganisationId != @org.Value && myFacilities.Count() > 0))
                    {
                        var orgNode = new MyNode()
                         {
                             text = org.Name,
                             value = org.Value,
                             icon = "glyphicon glyphicon-home",
                             backColor = "#ffffff",
                             color = "#428bca",
                             nodetype = MyNodeType.Organisation
                         };
                        orgNode.nodes = new List<MyNode>();


                        foreach (var @fac in myFacilities)
                        {
                            if (@fac.Value != selectedPrimaryAsInt)
                            {

                                var facNode = new MyNode
                                {
                                    parent = orgNode.value,
                                    text = fac.Name,
                                    value = fac.Value,
                                    icon = "glyphicon glyphicon-th-list",
                                    backColor = "#ffffff",
                                    color = "#66512c",
                                    nodetype = MyNodeType.Facility
                                };
                                if (participantsIds != null && selectExisting)
                                {
                                    if (ChatGroupController.CheckIfMatchingMyFacilityExists(participantsIds, facNode) !=
                                        null)
                                    {
                                        facNode.state = new state
                                        {
                                            @checked = true,
                                            disabled = false,
                                            expanded = true,
                                            selected = false
                                        };
                                    }
                                }
                                orgNode.nodes.Add(facNode);

                            }
                        }
                        nodes.Add(orgNode);
                    }
                }

            }
            return Json(nodes, JsonRequestBehavior.AllowGet);
        }

        public string UpdateUser(UserLoginInformation ulinfo)
        {
            try
            {
                var number = GenerateOTP(11);                
                var loggedinUser = LoginController.ActiveUser;
                ulinfo.ModifiedById = loggedinUser.USERID.ToString();
                if (ulinfo.USERID == 0)
                {
                    ulinfo.CreatedById = loggedinUser.USERID.ToString();
                    ulinfo.CreatedDate = DateTime.Now;
                    ulinfo.Status = 1;  //insert record status
                    if (ulinfo.IsActive)
                    {
                        ulinfo.UserIsActive = true;
                    }
                    try
                    {
                        using (var transactionScope = new TransactionScope())
                        {
                            try
                            {
                                using (var db = new kryptoEntities1()) // Context object
                                {
                                    db.UserLoginInformations.Add(ulinfo);
                                    db.SaveChanges();

                                    if (ulinfo.USERID > 0)
                                    {
                                        if (ulinfo.IsActive)
                                        {
                                            ulinfo.ActivatedDate = DateTime.Now;
                                            db.Entry(ulinfo).State = EntityState.Modified;
                                            db.SaveChanges();
                                            var useracive = new UserActivate
                                            {
                                                CreatedById = loggedinUser.USERID.ToString(),
                                                Date = DateTime.Now,
                                                USERID = ulinfo.USERID,
                                                IsActive = ulinfo.IsActive,
                                                Status = 1
                                            };
                                            db.UserActivates.Add(useracive);
                                            db.SaveChanges();
                                        }
                                        Encryption eny = new Encryption();
                                        if (ulinfo.HiddenValue != null && ulinfo.HiddenValue == "1")
                                        {
                                            string EncrptPassword = eny.EncryptString(number);
                                            ulinfo.Password = EncrptPassword;
                                            ulinfo.IsOrganisationAdmin = true;
                                            ulinfo.IsNormalUser = false;
                                        }
                                        if (ulinfo.FacHiddenValue != null && ulinfo.FacHiddenValue == "2")
                                        {
                                            string EncrptPassword = eny.EncryptString(number);
                                            ulinfo.Password = EncrptPassword;
                                            ulinfo.IsFacilityAdmin = true;
                                            ulinfo.IsNormalUser = false;

                                        }
                                        db.Entry(ulinfo).State = EntityState.Modified;
                                        db.SaveChanges();
                                    }

                                    if (ulinfo.HiddenValue != null && ulinfo.HiddenValue == "1")
                                    {

                                        string[] OrgIds = ulinfo.OrganizationsIds;

                                        if (ulinfo.USERID > 0 &&
                                           (OrgIds != null && OrgIds.Length > 0))
                                        {
                                            foreach (var eachid in OrgIds)
                                            {
                                                var orgid = eachid.ToString();
                                                db.OrganizationAdmins.Add(new OrganizationAdmin
                                                {
                                                    OrganisationId = int.Parse(orgid),
                                                    USERID = ulinfo.USERID,
                                                    STATUS = 1,
                                                    CREATED_DATE = DateTime.Now,
                                                    CreatedById = loggedinUser.USERID.ToString(),
                                                    ModifiedDate = DateTime.Now,
                                                    ModifiedById = loggedinUser.USERID.ToString()
                                                });
                                            }
                                            db.SaveChanges();
                                        }
                                    }

                                    var otherFacilityIds = ulinfo.OtherFacilityIds;
                                      
                                    if (ulinfo.USERID > 0 &&
                                        (otherFacilityIds != null && otherFacilityIds.Length > 0))
                                    {
                                        foreach (var eachid in otherFacilityIds)
                                        {
                                            var facilityid = int.Parse(eachid);
                                            db.UserFacilities.Add(new UserFacility
                                            {
                                                FacilityId = facilityid,
                                                USERID = ulinfo.USERID,
                                                Status = 1,
                                                CreatedById = loggedinUser.USERID.ToString(),
                                                CreatedDate = DateTime.Now,
                                                ModifiedDate = DateTime.Now,
                                                ModifiedById = loggedinUser.USERID.ToString()
                                            });
                                        }
                                        db.SaveChanges();
                                    }


                                   // var otherFacilityIds = ulinfo.OtherFacilityIds;
                                    if (ulinfo.FacHiddenValue != null && ulinfo.FacHiddenValue == "2")
                                    {
                                        db.FacilityAdmins.Add(new FacilityAdmin
                                        {
                                            FacilityMasterId = ulinfo.FacilityId.Value,
                                            USERID = ulinfo.USERID,
                                            STATUS = 1,
                                            CreatedById = loggedinUser.USERID.ToString(),
                                            CREATED_DATE = DateTime.Now,
                                            ModifiedDate = DateTime.Now,
                                            ModifiedById = loggedinUser.USERID.ToString()
                                        });
                                          db.SaveChanges();
                                    if (ulinfo.USERID > 0 &&
                                        (otherFacilityIds != null && otherFacilityIds.Length > 0))
                                    {
                                        foreach (var eachid in otherFacilityIds)
                                        {
                                            var facilityid = int.Parse(eachid);
                                            db.FacilityAdmins.Add(new FacilityAdmin
                                            {
                                                FacilityMasterId = facilityid,
                                                USERID = ulinfo.USERID,
                                                STATUS = 1,
                                                CreatedById = loggedinUser.USERID.ToString(),
                                                CREATED_DATE = DateTime.Now,
                                                ModifiedDate = DateTime.Now,
                                                ModifiedById = loggedinUser.USERID.ToString()
                                            });
                                        }
                                       
                                        db.SaveChanges();
                                    }
                                    }

                                    if (ulinfo.USERID > 0)
                                    {
                                        var initiallogin =
                                            new UserRegitrationForInitialLogin
                                            {
                                                USERID = ulinfo.USERID,
                                                Createdate = DateTime.Now,
                                                IsInitialLogin = true,
                                                IsTermsAccepted = false,
                                                IsSecQuestEnabled = false,
                                                IsMpinCreated = false,
                                                IsPasswordUpdated = false,
                                                Status = 1,
                                                CreatedById = loggedinUser.USERID.ToString(),
                                                ModifiedById = loggedinUser.USERID.ToString()
                                            };
                                        initiallogin.ModifiedDate = initiallogin.Createdate;
                                        db.UserRegitrationForInitialLogins.Add(initiallogin);
                                        db.SaveChanges();
                                    }
                                    if (ulinfo.HiddenValue == "1" || ulinfo.FacHiddenValue == "2")
                                    {
                                        SendOTPMail(number, ulinfo.EmailId, ulinfo.FirstName);
                                    }
                                }
                                transactionScope.Complete(); // transaction complete
                            }
                            catch (Exception ee)
                            {
                                return "FAIL";
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        return "FAIL";
                    }
                }
                else
                {
                    try
                    {
                        using (var transactionScope = new TransactionScope())
                        {
                            try
                            {
                                using (var db = new kryptoEntities1())
                                {
                                    ulinfo.ModifiedDate = DateTime.Now;
                                    var prevobj = _context.UserLoginInformations.Find(ulinfo.USERID);
                                    if (prevobj.IsActive != ulinfo.IsActive)
                                    {
                                        var activate = new UserActivate
                                        {
                                            IsActive = !prevobj.IsActive,
                                            CreatedById = loggedinUser.USERID.ToString()
                                        };
                                        if (ulinfo.IsActive)
                                        {
                                            ulinfo.ActivatedDate = DateTime.Now;
                                            activate.Date = ulinfo.ActivatedDate;
                                        }
                                        else
                                        {
                                            ulinfo.DeactivatedDate = DateTime.Now;
                                            activate.Date = ulinfo.DeactivatedDate;
                                        }
                                        activate.USERID = prevobj.USERID;
                                        db.UserActivates.Add(activate);
                                    }
                                    ulinfo.UserIsActive = ulinfo.IsActive;
                                    ulinfo = Updateobject(ulinfo.USERID, ulinfo);
                                    db.Entry(ulinfo).State = EntityState.Modified;
                                    db.SaveChanges();

                                    var otherFacilityIdsAsints = ulinfo.GetOtherFacilityIdsAsints();
                                    var facilityIdsInUserFacilityList = ulinfo.GetFacilityIdsInUserFacilityList();
                                    var toAdd = ExcludedRight(facilityIdsInUserFacilityList, otherFacilityIdsAsints);
                                    var toDelete = ExcludedLeft(facilityIdsInUserFacilityList, otherFacilityIdsAsints);
                                    foreach (var @id in toAdd)
                                    {
                                        db.UserFacilities.Add(new UserFacility
                                        {
                                            FacilityId = @id,
                                            USERID = ulinfo.USERID,
                                            Status = 1,
                                            CreatedById = loggedinUser.USERID.ToString(),
                                            CreatedDate = DateTime.Now,
                                            ModifiedDate = DateTime.Now,
                                            ModifiedById = loggedinUser.USERID.ToString()
                                        });
                                    }
                                    foreach (
                                        var existingUserFacility in
                                            toDelete.Select(
                                                id =>
                                                    db.UserFacilities.SingleOrDefault(
                                                        x =>
                                                            x.FacilityId.Value.Equals(id) &&
                                                            x.USERID.Equals(ulinfo.USERID))))
                                    {
                                        db.UserFacilities.Remove(existingUserFacility);
                                    }
                                    db.SaveChanges();
                                    if (ulinfo.IsActive == false)
                                    {
                                        RemoveUser(ulinfo.USERID);
                                    }
                                }
                                transactionScope.Complete();
                            }
                            catch (Exception ee)
                            {
                                return "FAIL";
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        return "FAIL";
                    }
                }
                if (ulinfo.USERID > 0 && ulinfo.IsActive)
                {
                    OtpSent(ulinfo, loggedinUser.USERID);
                }
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage);
                    }
                }
                return "FAIL";
            }
            return "SUCESS";
        }

        public int DeleteSingleuserRecord(int selecteduser)
        {
            var user = _context.UserLoginInformations.Single(x => x.USERID.Equals(selecteduser));
            if (user.IsSuperAdmin || user.IsOrganisationAdmin || user.IsFacilityAdmin)
            {
               
                return 1;
            }
            var chatgroupparticipants = _context.ChatGroupParticipants.FirstOrDefault(x => x.USERID == user.USERID);
            if (chatgroupparticipants == null)
            {
                user.Status = 2;
                _context.Entry(user).State = EntityState.Modified;
                _context.SaveChanges();
                return 3;
            }
            return 2;

            //set the status 2 for Deleting Record

            //List<UserFacility> faclist = (from uf in _context.UserFacilities
            //                              where uf.USERID == selecteduser
            //                              select uf).ToList();

            //foreach (UserFacility removeeachfac in faclist)
            //{
            //    removeeachfac.Status = 2; //set status 2 for deleting record
            //    _context.Entry(removeeachfac).State = EntityState.Modified;

            //}
        }

        public ActionResult CreateNew()
        {
            TempData["OpenCreateUser"] = true;
            return RedirectToAction("List", "UserDatatables");
        }

        public ActionResult CheckIfValidEmail(string email2)
        {
            var strings = email2.Split(new[] { "||||" }, StringSplitOptions.None);
            UserLoginInformation res;
            var email = strings[0];
            var userid = int.Parse(strings[1]);
            if (userid == 0)
                res = _context.UserLoginInformations.SingleOrDefault(x => x.EmailId == email && x.Status == 1);
            else
                res =
                    _context.UserLoginInformations.SingleOrDefault(
                        x => x.EmailId == email && x.USERID != userid && x.Status == 1);
            if (res != null) return Json("Email Id Already Exists.Use Another Email Id", JsonRequestBehavior.AllowGet);
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CheckIfValidContactNumber(string phonenumber)
        {
            var strings = phonenumber.Split(new[] { "||||" }, StringSplitOptions.None);
            UserLoginInformation res;
            var phonenum = strings[0];
            var userid = int.Parse(strings[1]);
            if (userid == 0)
                res = _context.UserLoginInformations.SingleOrDefault(x => x.ContactNumber == phonenum && x.Status == 1);
            else
                res =
                    _context.UserLoginInformations.SingleOrDefault(
                        x => x.ContactNumber == phonenum && x.USERID != userid && x.Status == 1);
            if (res != null)
                return Json("Contact Number Already Exists.Use Another Contact Number", JsonRequestBehavior.AllowGet);
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        public ActionResult checkifvalidZipcode(string zipcode)
        {
            if (zipcode.Length == 5)
            {
                var zipcodeitem = _context.ZipCodes.FirstOrDefault(x => x.ZipCode1 == zipcode);
                if (zipcodeitem != null)
                {
                    return Json(true, JsonRequestBehavior.AllowGet);
                }
            }
            return Json("Please Enter valid ZipCode", JsonRequestBehavior.AllowGet);
        }

        private static Random random = new Random();

        public static string GenerateOTP(int length)
        {
            var krypto = new kryptoEntities1();
            var res =
                krypto.Database.SqlQuery<int>(string.Format("SELECT * from [dbo].fnotpgenerationAdmin({0});", length))
                    .ToList();
            return res[0].ToString();
        }

        public bool SendOTPMail(string sOTP, string sToemailId, string sFirstName)
        {
            var flag = false;
            var sSmtpServer = WebConfigurationManager.AppSettings["SMTPSERVER"] ?? string.Empty;
            var sFromEmail = WebConfigurationManager.AppSettings["MAIL_FRM"] ?? string.Empty;
            var sUserId = WebConfigurationManager.AppSettings["MAIL_USR"] ?? string.Empty;
            var sPassword = WebConfigurationManager.AppSettings["MAIL_PWD"] ?? string.Empty;
            var sMailSubject = WebConfigurationManager.AppSettings["REGISTRATION_PASS_MAIL_SUBJECT"] ?? string.Empty;
            try
            {
                var msg = "Your KryptosText Application One Time Password (OTP) is   " + sOTP;
                var strMailBody = new StringBuilder();
                strMailBody.Append("<table><tr><td> Dear:" + sFirstName + "<br><br></td>");
                strMailBody.Append("</tr>");
                strMailBody.Append("<tr><td>" + msg + "<br></td></tr>");

                //"    For your security, please change the password once logged in using the new OTP.Validity of OTP is 30 Mins only."
                strMailBody.Append(
                    "<br><tr><td>For your security reasons, please change the password once logged in using the new OTP.</td></tr>");
                strMailBody.Append("<tr><td>Validity of OTP is 24 Mins only.<br></td></tr>");
                strMailBody.Append(
                    "<br><tr><td>Note: This is an auto generated mail please do not reply this mail.</td></tr><br>");
                strMailBody.Append("<br><tr><td> Support Team</td></tr>");
                strMailBody.Append("<tr><td> KryptosText.com</td></tr>");
                strMailBody.Append("</table> ");

                var objMail = new clsEMailHelper();
                objMail.MailFrom = sFromEmail;
                objMail.SMTPServer = sSmtpServer;
                objMail.UserID = sFromEmail;
                objMail.Password = sPassword;
                objMail.MailTo = sToemailId;
                objMail.Subject = sMailSubject.Replace("$DATE$", DateTime.Now.ToString(CultureInfo.InvariantCulture));
                objMail.Body = strMailBody;
                objMail.IsBodyHtml = true;
                flag = objMail.SendMail();
            }
            catch (Exception ex)
            {
            }
            return flag;
        }

        public string ResetPswd(int selecteduser)
        {
            var user = _context.UserLoginInformations.Single(x => x.USERID.Equals(selecteduser));
            var info = new KPTY_USER_FORGOT_PASS_OTP_REQ_TBL();
            var intialLogin = new UserRegitrationForInitialLogin();

            Session["OTPCount"] = 0;
            var otp = GenerateOTP(4);
            try
            {
                var loggedinUser = LoginController.ActiveUser;
                info.USERID = user.USERID;
                info.ModifiedById = loggedinUser.USERID.ToString();
                info.CreatedById = loggedinUser.USERID.ToString();
                info.CREATED_DATE = DateTime.Now;
                info.ModifiedDate = DateTime.Now;
                info.STATUS = 1;
                info.OTPVAL = otp;
                try
                {
                    using (var transactionScope = new TransactionScope())
                    {
                        try
                        {
                            using (var db = new kryptoEntities1()) // Context object
                            {
                                db.Database.ExecuteSqlCommand(
                                    "delete from KPTY_USER_FORGOT_PASS_OTP_REQ_TBL where UserId = {0}", selecteduser);
                                db.KPTY_USER_FORGOT_PASS_OTP_REQ_TBL.Add(info);
                                db.UserRegitrationForInitialLogins.Remove(
                                    db.UserRegitrationForInitialLogins.SingleOrDefault(u => u.USERID == selecteduser));

                                intialLogin.IsInitialLogin = true;
                                intialLogin.IsTermsAccepted = false;
                                intialLogin.IsSecQuestEnabled = false;
                                intialLogin.IsPasswordUpdated = false;
                                intialLogin.IsMpinCreated = false;
                                intialLogin.Notes = null;
                                intialLogin.Status = 1;
                                intialLogin.ModifiedById = loggedinUser.USERID.ToString();
                                intialLogin.ModifiedDate = DateTime.Now;
                                intialLogin.USERID = selecteduser;
                                intialLogin.Createdate = DateTime.Now;
                                intialLogin.CreatedById = loggedinUser.USERID.ToString();

                                db.UserRegitrationForInitialLogins.Add(intialLogin);
                                db.SaveChanges();
                                db.Database.ExecuteSqlCommand("delete from KPTY_USER_SECQURITY_QUEST_ANS where UserId = {0}", selecteduser);
                                Encryption eny = new Encryption();
                                string EncrptPassword = eny.EncryptString(otp.ToString());
                                user.Password = EncrptPassword;
                                user.ModifiedById = loggedinUser.USERID.ToString();
                                user.ModifiedDate = DateTime.Now;
                                db.Entry(user).State = EntityState.Modified;
                                db.SaveChanges();
                            }
                            transactionScope.Complete();
                            // transaction complete
                            var recemail = user.EmailId;
                            if (!SendOTPMail(otp, recemail, user.FirstName))
                                return "Invalid Email";
                        }
                        catch (Exception ee)
                        {
                            return "FAIL";
                        }
                    }
                }
                catch (Exception exception)
                {
                    return "FAIL";
                }
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage);
                    }
                }
                return "FAIL";
            }

            return "SUCESS";
        }

        public string ResetOtp(int selectedUser)
        {
            var loggedinUser = LoginController.ActiveUser;
            var user = _context.UserLoginInformations.Single(x => x.USERID.Equals(selectedUser));
            var userOtp = new UserForgotMpinsOTP();
            var number = GenerateOTP(11);
            try
            {
                using (var transactionScope = new TransactionScope())
                {
                    try
                    {
                        using (var db = new kryptoEntities1()) // Context object
                        {
                            userOtp.USERID = selectedUser;
                            userOtp.OTPVAL = number;
                            userOtp.STATUS = 2;
                            userOtp.Notes = "";
                            userOtp.CREATED_DATE = DateTime.Now;
                            userOtp.ModifiedDate = DateTime.Now;
                            userOtp.CreatedById = loggedinUser.USERID.ToString();
                            userOtp.ModifiedById = loggedinUser.USERID.ToString();

                            db.Database.ExecuteSqlCommand("delete from UserForgotMpinsOTPS where UserId = {0}",
                                selectedUser);

                            db.UserForgotMpinsOTPS.Add(userOtp);
                            db.SaveChanges();
                        }

                        transactionScope.Complete();



                        bool x1 = SendOTPMail(number, user.EmailId, user.FirstName);
                        if (!x1)
                            return "Invalid Email";
                    }
                    catch (Exception ee)
                    {
                        return "FAIL";
                    }
                }
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage);
                    }
                }
                return "FAIL";
            }

            return "SUCESS";
        }

        public void OtpSent(UserLoginInformation ulinfo, int loginUserId)
        {
            var user = _context.UserRegistrationOTPs.SingleOrDefault(u => u.USERID.Equals(ulinfo.USERID));
            if (user == null)
            {
                var number = GenerateOTP(11);
                using (var transactionscope = new TransactionScope())
                {
                    bool value;
                    try
                    {
                        using (var db = new kryptoEntities1()) // Context object
                        {
                            try
                            {
                                var usRegOtp = new UserRegistrationOTP
                                {
                                    USERID = ulinfo.USERID,
                                    OTP = number,
                                    Status = 2,
                                    Notes = "",
                                    CreatedById = loginUserId.ToString(),
                                    ModifiedById = loginUserId.ToString(),
                                    CreatedDate = DateTime.Now,
                                    ModifiedDate = DateTime.Now
                                };
                                db.UserRegistrationOTPs.Add(usRegOtp);
                                db.SaveChanges();
                                value = true;
                                transactionscope.Complete();
                            }
                            catch (Exception)
                            {
                                value = false;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        value = false;
                    }
                    if (value)
                        SendOTPMail(number, ulinfo.EmailId, ulinfo.FirstName + " " + ulinfo.LastName);
                }
            }
        }

        public void RemoveUser(int userId)
        {
            _context.Database.ExecuteSqlCommand("delete from UserRegistrationOTP where UserId = {0}", userId);
        }
    }
}