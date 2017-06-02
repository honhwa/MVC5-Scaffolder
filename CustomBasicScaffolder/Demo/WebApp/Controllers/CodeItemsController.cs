﻿using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Repository.Pattern.UnitOfWork;
using Repository.Pattern.Infrastructure;
using WebApp.Models;
using WebApp.Services;
using WebApp.Repositories;
using WebApp.Extensions;


namespace WebApp.Controllers
{
    public class CodeItemsController : Controller
    {

        //Please RegisterType UnityConfig.cs
        //container.RegisterType<IRepositoryAsync<CodeItem>, Repository<CodeItem>>();
        //container.RegisterType<ICodeItemService, CodeItemService>();

        //private StoreContext db = new StoreContext();
        private readonly ICodeItemService _codeItemService;
        private readonly IUnitOfWorkAsync _unitOfWork;

        public CodeItemsController(ICodeItemService codeItemService, IUnitOfWorkAsync unitOfWork)
        {
            _codeItemService = codeItemService;
            _unitOfWork = unitOfWork;
        }

        // GET: CodeItems/Index
        public async Task<ActionResult> Index()
        {

            var codeitems = _codeItemService.Queryable().Include(c => c.BaseCode);


            return View(await codeitems.ToListAsync());

        }

        // Get :CodeItems/PageList
        // For Index View Boostrap-Table load  data 
        [HttpGet]
        public async Task<ActionResult> GetData(int page = 1, int rows = 10, string sort = "Id", string order = "asc", string filterRules = "")
        {
            var filters = JsonConvert.DeserializeObject<IEnumerable<filterRule>>(filterRules);
            int totalCount = 0;
            //int pagenum = offset / limit +1;
            var codeitems = await _codeItemService
                .Query(new CodeItemQuery().Withfilter(filters)).Include(c => c.BaseCode)
                .OrderBy(n => n.OrderBy(sort, order))
                .SelectPageAsync(page, rows, out totalCount);



            var datarows = codeitems.Select(n => new { BaseCodeCodeType = (n.BaseCode == null ? "" : n.BaseCode.CodeType), Id = n.Id, Code = n.Code, Text = n.Text, BaseCodeId = n.BaseCodeId }).ToList();
            var pagelist = new { total = totalCount, rows = datarows };
            return Json(pagelist, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> SaveData(CodeItemChangeViewModel codeitems)
        {
            if (codeitems.updated != null)
            {
                foreach (var updated in codeitems.updated)
                {
                    _codeItemService.Update(updated);
                }
            }
            if (codeitems.deleted != null)
            {
                foreach (var deleted in codeitems.deleted)
                {
                    _codeItemService.Delete(deleted);
                }
            }
            if (codeitems.inserted != null)
            {
                foreach (var inserted in codeitems.inserted)
                {
                    _codeItemService.Insert(inserted);
                }
            }
            await _unitOfWork.SaveChangesAsync();

            return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> GetBaseCodes()
        {
            var basecodeRepository = _unitOfWork.RepositoryAsync<BaseCode>();
            var data = await basecodeRepository.Queryable().ToListAsync();
            var rows = data.Select(n => new { Id = n.Id, CodeType = n.CodeType });
            return Json(rows, JsonRequestBehavior.AllowGet);
        }



        // GET: CodeItems/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var codeItem = await _codeItemService.FindAsync(id);

            if (codeItem == null)
            {
                return HttpNotFound();
            }
            return View(codeItem);
        }


        // GET: CodeItems/Create
        public ActionResult Create()
        {
            var codeItem = new CodeItem();
            //set default value
            var basecodeRepository = _unitOfWork.RepositoryAsync<BaseCode>();
            ViewBag.BaseCodeId = new SelectList(basecodeRepository.Queryable(), "Id", "CodeType");
            return View(codeItem);
        }

        // POST: CodeItems/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "BaseCode,Id,Code,Text,BaseCodeId")] CodeItem codeItem)
        {
            if (ModelState.IsValid)
            {
                _codeItemService.Insert(codeItem);
                await _unitOfWork.SaveChangesAsync();
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                }
                DisplaySuccessMessage("Has append a CodeItem record");
                return RedirectToAction("Index");
            }
            else
            {
                var modelStateErrors = String.Join("", this.ModelState.Keys.SelectMany(key => this.ModelState[key].Errors.Select(n => n.ErrorMessage)));
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, err = modelStateErrors }, JsonRequestBehavior.AllowGet);
                }
                DisplayErrorMessage(modelStateErrors);
            }
            var basecodeRepository = _unitOfWork.RepositoryAsync<BaseCode>();
            ViewBag.BaseCodeId = new SelectList(await basecodeRepository.Queryable().ToListAsync(), "Id", "CodeType", codeItem.BaseCodeId);

            return View(codeItem);
        }

        // GET: CodeItems/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var codeItem = await _codeItemService.FindAsync(id);
            if (codeItem == null)
            {
                return HttpNotFound();
            }
            var basecodeRepository = _unitOfWork.RepositoryAsync<BaseCode>();
            ViewBag.BaseCodeId = new SelectList(basecodeRepository.Queryable(), "Id", "CodeType", codeItem.BaseCodeId);
            return View(codeItem);
        }

        // POST: CodeItems/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "BaseCode,Id,Code,Text,BaseCodeId")] CodeItem codeItem)
        {
            if (ModelState.IsValid)
            {
                codeItem.ObjectState = ObjectState.Modified;
                _codeItemService.Update(codeItem);

                await _unitOfWork.SaveChangesAsync();
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                }
                DisplaySuccessMessage("Has update a CodeItem record");
                return RedirectToAction("Index");
            }
            else
            {
                var modelStateErrors = String.Join("", this.ModelState.Keys.SelectMany(key => this.ModelState[key].Errors.Select(n => n.ErrorMessage)));
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, err = modelStateErrors }, JsonRequestBehavior.AllowGet);
                }
                DisplayErrorMessage(modelStateErrors);
            }
            var basecodeRepository = _unitOfWork.RepositoryAsync<BaseCode>();
            ViewBag.BaseCodeId = new SelectList(await basecodeRepository.Queryable().ToListAsync(), "Id", "CodeType", codeItem.BaseCodeId);

            return View(codeItem);
        }

        // GET: CodeItems/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var codeItem = await _codeItemService.FindAsync(id);
            if (codeItem == null)
            {
                return HttpNotFound();
            }
            return View(codeItem);
        }

        // POST: CodeItems/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var codeItem = await _codeItemService.FindAsync(id);
            _codeItemService.Delete(codeItem);
            await _unitOfWork.SaveChangesAsync();
            if (Request.IsAjaxRequest())
            {
                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            DisplaySuccessMessage("Has delete a CodeItem record");
            return RedirectToAction("Index");
        }






        //导出Excel
        [HttpPost]
        public ActionResult ExportExcel(string filterRules = "", string sort = "Id", string order = "asc")
        {
            var fileName = "codeitems_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";
            var stream = _codeItemService.ExportExcel(filterRules, sort, order);
            return File(stream, "application/vnd.ms-excel", fileName);

        }



        private void DisplaySuccessMessage(string msgText)
        {
            TempData["SuccessMessage"] = msgText;
        }

        private void DisplayErrorMessage(string msgText)
        {
            TempData["ErrorMessage"] = msgText;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _unitOfWork.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}