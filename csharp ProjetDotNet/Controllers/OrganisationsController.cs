// ...
[HttpPost, ActionName("Delete")]
[ValidateAntiForgeryToken]
public IActionResult DeleteConfirmed(int id)
{
    using var transaction = _context.Database.BeginTransaction();
    try
    {
        // charger l'organisation avec ses enfants
        var org = _context.Organisations
            .Include(o => o.Projets)
            .Include(o => o.Equipes)
            .FirstOrDefault(o => o.OrgID == id);

        if (org == null) return NotFound();

        // supprimer projets puis équipes (ou inverse selon contraintes)
        if (org.Projets != null && org.Projets.Any())
            _context.Projets.RemoveRange(org.Projets);

        if (org.Equipes != null && org.Equipes.Any())
            _context.Equipes.RemoveRange(org.Equipes);

        // supprimer l'organisation
        _context.Organisations.Remove(org);

        _context.SaveChanges();
        transaction.Commit();
    }
    catch (Exception ex)
    {
        transaction.Rollback();
        _logger.LogError(ex, "Erreur lors de la suppression de l'organisation ID {Id}", id);
        ModelState.AddModelError(string.Empty, "Impossible de supprimer l'organisation : " + ex.Message);
        var orgView = _context.Organisations.Find(id);
        return View("~/Views/Organisations/Delete.cshtml", orgView);
    }

    return RedirectToAction(nameof(Index));
}