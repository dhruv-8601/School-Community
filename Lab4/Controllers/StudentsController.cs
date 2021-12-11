using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Lab4.Data;
using Lab4.Models;
using Lab4.Models.ViewModels;

namespace Lab4.Controllers
{
    public class StudentsController : Controller
    {
        private readonly SchoolCommunityContext _context;

        public StudentsController(SchoolCommunityContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? id)
        {
            var studentViewModel = new CommunityViewModel();
            studentViewModel.Students = await _context.Students
                .Include(student => student.CommunityMemberships)
                .ThenInclude(student => student.Community)
                .AsNoTracking()
                .ToListAsync();
            
            if(id != null)
            {
                ViewData["StudentID"] = id;
                studentViewModel.CommunityMemberships = studentViewModel.Students.Where(student => student.ID == id).Single().CommunityMemberships;
            }

            return View(studentViewModel);
        }

        public async Task<IActionResult> EditMemberships(int? id)
        {
            if(id == null)
            {
                return NotFound();
            }
            CommunityViewModel studentViewModel = new CommunityViewModel();
            studentViewModel.CommunityMemberships = await _context.CommunityMemberships.Where(communityMember => communityMember.StudentID == id).ToListAsync();
            studentViewModel.Students = await _context.Students.Where(student => student.ID == id).ToListAsync();
            studentViewModel.Communities = await _context.Communities.ToListAsync();
            return View(studentViewModel);
        }

        public async Task<IActionResult> AddMemberships(int? id, string communityId)
        {
            if(id == null || String.IsNullOrEmpty(communityId))
            {
                return NotFound();
            }
            var s = await _context.Students.FindAsync(id);
            var c = await _context.Communities.FindAsync(communityId);
            if(s == null || c == null)
            {
                return NotFound();
            }
            var createCommunityMembership = new CommunityMembership();
            createCommunityMembership.StudentID = (int)id;
            createCommunityMembership.CommunityID = communityId;
            _context.CommunityMemberships.Add(createCommunityMembership);
            await _context.SaveChangesAsync();

            return RedirectToAction("EditMemberships", new { id = id });
        }

        public async Task<IActionResult> DeleteMemberships(int? id, string communityId)
        {
            if (id == null || String.IsNullOrEmpty(communityId))
            {
                return NotFound();
            }
            var community = await _context.CommunityMemberships.FindAsync(id, communityId);
            if (community == null)
            {
                return NotFound();
            }
            _context.CommunityMemberships.Remove(community);
            await _context.SaveChangesAsync();

            return RedirectToAction("EditMemberships", new { id = id });
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(m => m.ID == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,LastName,FirstName,EnrollmentDate")] Student student)
        {
            if (ModelState.IsValid)
            {
                _context.Add(student);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(student);
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,LastName,FirstName,EnrollmentDate")] Student student)
        {
            if (id != student.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(student);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(student);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(m => m.ID == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.ID == id);
        }
    }
}