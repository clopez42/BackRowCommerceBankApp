﻿using BackRowCommerceApp.Data;
using BackRowCommerceApp.Infrastructure;
using BackRowCommerceApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace BackRowCommerceApp.Controllers
{
    public class TransactionController : Controller
    {
        private ApplicationDbContext _db;

        public TransactionController(ApplicationDbContext db)
        {
            _db = db;
        }
        public IActionResult Index()
        {
            IEnumerable<Transaction> objTransactionList = _db.Transactions;
            return View(objTransactionList);
        }

        //GET action method
        public IActionResult Create()
        {
            return View();
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Transaction obj)
        {
            var user = _db.UserInfo.FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                UserInfo userInfo = new UserInfo
                {
                    AccountNum = AccountNumberGenerator(),
                    UserName = User.Identity.Name,
                    Balance = 0,
                    Location = Constants.States.MO
                };
                _db.UserInfo.Add(userInfo);
                _db.SaveChanges();
                user = _db.UserInfo.FirstOrDefault(u => u.UserName == User.Identity.Name);
            }
            obj.UserName = user.UserName;

            if (ModelState.IsValid)
            {
                if (obj.CR_DR == Constants.TransactionType.CR)
                {
                    var newBalance = user.Balance + obj.Amount;
                    user.Balance = newBalance;
                    _db.UserInfo.Update(user);
                }
                else if (obj.CR_DR == Constants.TransactionType.DR)
                {
                    var newBalance = user.Balance - obj.Amount;
                    user.Balance = newBalance;
                    _db.UserInfo.Update(user);
                }
                obj.AccountNum = user.AccountNum;
                obj.Balance = user.Balance;
                _db.Transactions.Add(obj);
                _db.SaveChanges();

                GenerateNotification(obj);
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        private int AccountNumberGenerator()
        {
            Random random = new Random();
            int accountNum = random.Next(123456789, 999999999);
            return accountNum;
        }

        public void GenerateNotification(Transaction obj)
        {
            Constants c = new Constants();
            string message = "";
            var userInfoFromDb = _db.UserInfo.FirstOrDefault(u => u.UserName == User.Identity.Name);
            var notificationSettingsFromDb = _db.NotificationSettings.FirstOrDefault(u => u.UserName == User.Identity.Name);

            if (notificationSettingsFromDb != null)
            {
                if (notificationSettingsFromDb.TransactionDescription == true)
                {
                    string description = "New transaction from " + obj.Description + ".";
                    message += description;
                }
                if (notificationSettingsFromDb.OutOfStateTransaction == true)
                {
                    var map = c.statesMap();
                    foreach(KeyValuePair<Constants.States,string> kvp in map)
                    {
                        if(kvp.Key == obj.Location)
                        {
                            message += "\nNew Transaction from " + kvp.Value + ".";
                        }
                    }
                    
                }
                if ((notificationSettingsFromDb.Withdrawal == true) && (obj.CR_DR == Constants.TransactionType.DR))
                {
                    string w = "\nNew Withdrawal of $" + obj.Amount.ToString() + ".";
                    message += w;
                }
                if ((notificationSettingsFromDb.Deposit == true) && (obj.CR_DR == Constants.TransactionType.DR))
                {
                    string d = "\nNew Deposit of $" + obj.Amount.ToString() + ".";
                    message += d;
                }
                if ((notificationSettingsFromDb.Overdraft == true) && (userInfoFromDb.Balance < 0))
                {
                    string o = "\nYour account has overdrafted.";
                    message += o;
                }
                if ((notificationSettingsFromDb.LessThan100 == true) && (userInfoFromDb.Balance < 100))
                {
                    string lowBal = "\nYour account balance is less than $100.";
                    message += lowBal;
                }
                if ((notificationSettingsFromDb.LessThan100 == true) || (notificationSettingsFromDb.OutOfStateTransaction == true)
                    || (notificationSettingsFromDb.Withdrawal == true) || (notificationSettingsFromDb.Deposit == true)
                    || (notificationSettingsFromDb.Overdraft == true) || (notificationSettingsFromDb.TransactionDescription == true))

                {
                    Notification notification = new Notification
                    {
                        UserName = User.Identity.Name,
                        Message = message,
                    };
                    _db.Notifications.Add(notification);
                    _db.SaveChanges();
                }
            }
        }
    }
}
