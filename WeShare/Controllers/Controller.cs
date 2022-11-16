using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models;

namespace WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class Controller : ControllerBase
{
    private readonly WeshareContext _context = new();

	/// <summary>
	///     Gets all user emails from the database.
	/// </summary>
	/// <returns>
	///     A List of all user emails.
	/// </returns>
	[HttpGet(nameof(GetUserEmails))]
    public async Task<ActionResult<List<string>>> GetUserEmails()
    {
        return await _context.Users.Select(u => u.Email).ToListAsync();
    }

    /// <summary>
    ///     Gets all deactivated user emails from the database.
    /// </summary>
    /// <returns>
    ///     A List of all deactivated user emails.
    /// </returns>
    [HttpGet(nameof(GetDeactivatedUserEmails))]
    public async Task<ActionResult<List<string>>> GetDeactivatedUserEmails()
    {
        var deactivatedUserIDs = await _context.DeactivatedUsers.Select(u => u.UserId).ToListAsync();
        return await _context.Users.Where(u => deactivatedUserIDs.Contains(u.Id)).Select(u => u.Email).ToListAsync();
    }

	/// <summary>
	///     Inserts a new user in the database.
	/// </summary>
	/// <param name="email"></param>
	/// <param name="firstName"></param>
	/// <param name="lastName"></param>
	/// <param name="phoneNumber"></param>
	[HttpPost(nameof(InsertUser)+"{email}/{firstName}/{lastName}/{phoneNumber}")]
	public void InsertUser(string email, string firstName, string lastName, string phoneNumber)
    {
        _context.Users.Add(new User
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber
        });
        _context.SaveChanges();
    }

    /// <summary>
    ///     Creates a new group in the database.
    ///     Automatically adds the creator to the group as the owner.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupName"></param>
    [HttpPost(nameof(InsertGroup)+"{userId}/{groupName}")]
    public void InsertGroup(int userId, string groupName)
    {
        _context.Groups.Add(new Group
        {
            Name = groupName,
            Date = DateTime.Now,
            Closed = false
        });
        _context.SaveChanges();
        _context.UserToGroups.Add(new UserToGroup
        {
            UserId = userId,
            GroupId = _context.Groups.OrderByDescending(g => g.Id).First().Id,
            IsOwner = true
        });
        _context.SaveChanges();
    }

    /// <summary>
    ///     Gets all the current payments for a group.
    /// </summary>
    /// <param name="groupId"></param>
    /// <returns></returns>
    [HttpGet(nameof(GetGroupPayments)+"{groupId}")]
    public IEnumerable<Payment> GetGroupPayments(int groupId)
    {
        var userToGroupIDs = _context.UserToGroups.Where(u => u.GroupId == groupId).Select(u => u.Id);
        return _context.Payments.ToList().Where(p => userToGroupIDs.Contains(p.UserToGroupId));
    }
    
    /// <summary>
    ///     Makes a payment for a user in a group.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <param name="amount"></param>
    [HttpPost(nameof(InsertPayment)+"{userId}/{groupId}/{amount}")]
    public void InsertPayment(int userId, int groupId, double amount)
    {
        var userToGroupId = _context.UserToGroups.First(u => u.UserId == userId && u.GroupId == groupId).Id;
        _context.Payments.Add(new Payment
        {
            UserToGroupId = userToGroupId,
            Amount = amount,
            Date = DateTime.Now,
        });
        _context.SaveChanges();
    }

    /// <summary>
    ///     Inserts a new friendship in the database.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="friendId"></param>
    [HttpPost(nameof(InsertFriendship)+"{userId}/{friendId}")]
    public void InsertFriendship(int userId, int friendId)
    {
        _context.Friendships.Add(new Friendship
        {
            User1Id = userId,
            User2Id = friendId
        });
        _context.SaveChanges();
    }

    /// <summary>
    ///     Gets all the friends of a user.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>
    ///     A List of all the friends of a user.
    /// </returns>
    [HttpGet(nameof(GetFriends)+"{userId}")]
    public IEnumerable<User> GetFriends(int userId)
    {
        var friendIds = _context.Friendships.Where(f => f.User1Id == userId).Select(f => f.User2Id);
        return _context.Users.ToList().Where(u => friendIds.Contains(u.Id));
    }

    /// <summary>
    ///     Inserts a new group invite in the database.
    ///     The invite will not be sent if:
    ///     The user is already in the group...
    ///     The user has already been invited to the group...
    ///     The user is deactivated...
    ///     The group is closed...
    ///     The group has a value in the ToBePaid table...
    ///     The sender is not the owner of the group...
    /// </summary>
    /// <param name="senderId"></param>
    /// <param name="receiverId"></param>
    /// <param name="groupId"></param>
    [HttpPost(nameof(InsertGroupInvite)+"{senderId}/{receiverId}/{groupId}")]
    public void InsertGroupInvite(int senderId, int receiverId, int groupId)
    {
        if (_context.UserToGroups.Any(u => u.UserId == receiverId && u.GroupId == groupId))
            return;
        if (_context.Invites.Any(i => i.SenderId == senderId && i.ReceiverId == receiverId && i.GroupId == groupId))
            return;
        if (_context.DeactivatedUsers.Any(u => u.UserId == receiverId))
            return;
        if (_context.Groups.Any(g => g.Id == groupId && g.Closed))
            return;
        var userToGroupIds = _context.UserToGroups.Where(u => u.GroupId == groupId).Select(u => u.Id);
        if (_context.ToBePaids.Any(t => userToGroupIds.Contains(t.UserToGroupId)))
            return;
        if (!_context.UserToGroups.Any(u => u.UserId == senderId && u.GroupId == groupId && u.IsOwner))
            return;

        _context.Invites.Add(new Invite
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            GroupId = groupId
        });
        _context.SaveChanges();
    }

    /// <summary>
    ///     Removes a group invite from the database.
    /// </summary>
    /// <param name="receiverId"></param>
    /// <param name="groupId"></param>
    [HttpDelete(nameof(RemoveGroupInvite)+"{receiverId}/{groupId}")]
    public void RemoveGroupInvite(int receiverId, int groupId)
    {
        var invite = _context.Invites.FirstOrDefault(i => i.ReceiverId == receiverId && i.GroupId == groupId);
        if (invite == null) return;
        _context.Invites.Remove(invite);
        _context.SaveChanges();
    }

    /// <summary>
    ///     Inserts new values to ToBePaid table in the database.
    /// </summary>
    /// <param name="groupId"></param>
    /// <param name="userId"></param>
    [HttpPost(nameof(InsertToBePaid)+"{groupId}/{userId}")]
    public void InsertToBePaid(int groupId, int userId)
    {
        if (!_context.UserToGroups.Any(u => u.UserId == userId && u.GroupId == groupId && u.IsOwner))
            return;
		if (_context.ToBePaids.Any(t => t.UserToGroupId == _context.UserToGroups.First(u => u.UserId == userId && u.GroupId == groupId).Id))
			return;
		foreach (var userToGroupId in _context.UserToGroups.Where(u => u.GroupId == groupId).Select(u => u.Id))
        {
			_context.ToBePaids.Add(new ToBePaid
            {
                UserToGroupId = userToGroupId,
                Approved = false,
                Date = null
            });
        }
		
        _context.SaveChanges();

        // Remove all the invites for the group.
        foreach (var invite in _context.Invites.Where(i => i.GroupId == groupId))
            RemoveGroupInvite(invite.ReceiverId, invite.GroupId);
    }

	/// <summary>
	///     Approves a ToBePaid value in the database.
	/// </summary>
	/// <param name="userId"></param>
	/// <param name="groupId"></param>
	[HttpPut(nameof(ApproveToBePaid)+"{userId}/{groupId}")]
	public void ApproveToBePaid(int groupId, int userId)
    {
        var userToGroupId = _context.UserToGroups.FirstOrDefault(u => u.UserId == userId && u.GroupId == groupId).Id;
        var toBePaid = _context.ToBePaids.FirstOrDefault(t => t.UserToGroupId == userToGroupId);
        if (toBePaid == null) return;
        toBePaid.Approved = true;
        toBePaid.Date = DateTime.Now;
        _context.SaveChanges();
        // if all ToBePaid values are approved for the group
        var userToGroupIds = _context.UserToGroups.Where(u => u.GroupId == groupId).Select(u => u.Id);
        if (_context.ToBePaids.Where(t => userToGroupIds.Contains(t.UserToGroupId)).All(t => t.Approved))
            InsertReceipts(groupId);
    }

    /// <summary>
    ///     Inserts receipts for a group.
    /// </summary>
    /// <param name="groupId"></param>
    private void InsertReceipts(int groupId)
    {
        var userToGroupIds = _context.UserToGroups.Where(u => u.GroupId == groupId).Select(u => u.Id);
        // Get the total amount of money that the group has spent.
        var totalAmount = _context.Payments.Where(p => userToGroupIds.Contains(p.UserToGroupId)).Sum(p => p.Amount);
        // Get the number of users in the group.
        var numberOfUsers = _context.UserToGroups.Count(u => u.GroupId == groupId);
        // Get the amount that each user should pay.
        var amountPerUser = totalAmount / numberOfUsers;
        // Get the amount that each user has paid.
        _context.Payments.Where(p => userToGroupIds.Contains(p.UserToGroupId)).GroupBy(p => p.UserToGroupId)
            .Select(p => new { UserToGroupId = p.Key, Amount = p.Sum(x => x.Amount) }).ToList().ForEach(
                p => _context.Receipts.Add(new Receipt
                {
                    UserToGroupId = p.UserToGroupId,
                    Amount = p.Amount - amountPerUser, 
                    Fulfilled = false,
                }));
        _context.SaveChanges();
    }
    
    /// <summary>
    ///     Gets the receipt for a user in a group.
    /// </summary>
    /// <param name="groupId"></param>
    /// <param name="userId"></param>
    /// <returns>
    ///    The receipt for a user in a group.
    /// </returns>
    [HttpGet(nameof(GetReceipt)+"{groupId}/{userId}")]
    public Receipt GetReceipt(int groupId, int userId)
    {
        var userToGroupId = _context.UserToGroups.FirstOrDefault(u => u.UserId == userId && u.GroupId == groupId).Id;
        return _context.Receipts.FirstOrDefault(r => r.UserToGroupId == userToGroupId);
    }

    /// <summary>
    ///     Pays a receipt by deducting the amount from the user's balance and marking the receipt as fulfilled.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    [HttpPut(nameof(PayReceipt)+"{userId}/{groupId}")]
    public void PayReceipt(int groupId, int userId)
    {
        var receipt = _context.Receipts.FirstOrDefault(r => r.UserToGroupId == _context.UserToGroups.FirstOrDefault(u => u.UserId == userId && u.GroupId == groupId).Id);
        var balance = _context.Wallets.FirstOrDefault(w => w.UserId == userId).Balance;
        if (balance < receipt.Amount) return;
        _context.Wallets.FirstOrDefault(w => w.UserId == userId).Balance += receipt.Amount;
        receipt.Date = DateTime.Now;
        receipt.Fulfilled = true;
        _context.SaveChanges();
        
        // if all receipts are fulfilled for the group, close the group.
        var userToGroupIds = _context.UserToGroups.Where(u => u.GroupId == groupId).Select(u => u.Id);
        if (_context.Receipts.Where(r => userToGroupIds.Contains(r.UserToGroupId)).All(r => r.Fulfilled))
            CloseGroup(groupId);
    }
    
    /// <summary>
    ///     Closes a group.
    /// </summary>
    /// <param name="groupId"></param>
    private void CloseGroup(int groupId)
    {
        var group = _context.Groups.FirstOrDefault(g => g.Id == groupId);
        group.Closed = true;
        _context.SaveChanges();
    }
    
    /// <summary>
    ///     Removes a user from a group.
    /// </summary>
    /// <param name="groupId"></param>
    /// <param name="userId"></param>
    /// <param name="userToRemoveId"></param>
    [HttpDelete(nameof(RemoveUserFromGroup)+"{groupId}/{userId}/{userToRemoveId}")]
    public void RemoveUserFromGroup(int groupId, int userId, int userToRemoveId)
    {
        if (!_context.UserToGroups.Any(u => u.GroupId == groupId && u.UserId == userId && u.IsOwner))
            return;
		if (_context.ToBePaids.Any(t => t.UserToGroupId == _context.UserToGroups.FirstOrDefault(u => u.GroupId == groupId && u.UserId == userToRemoveId).Id))
			return;
		if (_context.Groups.FirstOrDefault(g => g.Id == groupId).Closed)
			return;
	    var userToGroup = _context.UserToGroups.FirstOrDefault(u => u.GroupId == groupId && u.UserId == userToRemoveId);
        _context.UserToGroups.Remove(userToGroup);
        _context.SaveChanges();
    }
    
    /// <summary>
    ///     Returns all receipts of a user.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>
    ///     A list of receipts.
    /// </returns>
    [HttpGet(nameof(GetReceipts)+"{userId}")]
    public IEnumerable<Receipt> GetReceipts(int userId)
    {
        return _context.Receipts.Where(r => r.UserToGroupId == _context.UserToGroups.FirstOrDefault(u => u.UserId == userId).Id).ToList();
    }
    
    /// <summary>
    ///     Creates a wallet of a random amount of money for a user.
    /// </summary>
    /// <param name="userId"></param>
    [HttpPost(nameof(InsertWallet)+"{userId}")]
    public void InsertWallet(int userId)
    {
        if (_context.Wallets.Any(w => w.UserId == userId)) return;
        _context.Wallets.Add(new Wallet
        {
            UserId = userId,
            Balance = new Random().Next(5000, 25000)
        });
        _context.SaveChanges();
    }
    
    /// <summary>
    ///     Deactivates a user until a given date.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="until"></param>
    [HttpPut(nameof(DeactivateUser)+"{userId}/{until}")]
    public void DeactivateUser(int userId, DateTime until)
    {
        if (_context.DeactivatedUsers.Any(d => d.UserId == userId)) return;
        _context.DeactivatedUsers.Add(new DeactivatedUser
        {
            UserId = userId,
            Until = until
        });
        _context.SaveChanges();
    }
    
    /// <summary>
    ///     Activates a user.
    /// </summary>
    /// <param name="userId"></param>
    [HttpPut(nameof(ActivateUser)+"{userId}")]
    public void ActivateUser(int userId)
    {
        if (!_context.DeactivatedUsers.Any(d => d.UserId == userId)) return;
        var deactivatedUser = _context.DeactivatedUsers.FirstOrDefault(d => d.UserId == userId);
        _context.DeactivatedUsers.Remove(deactivatedUser);
        _context.SaveChanges();
    }
    
    /// <summary>
    ///     Updates a user's information.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="email"></param>
    /// <param name="firstName"></param>
    /// <param name="lastName"></param>
    /// <param name="phoneNumber"></param>
    [HttpPut(nameof(UpdateUser)+"{userId}/{email}/{firstName}/{lastName}/{phoneNumber}")]
    public void UpdateUser(int userId, string email, string firstName, string lastName, string phoneNumber)
    {
        var user = _context.Users.FirstOrDefault(u => u.Id == userId);
        user.Email = email;
        user.FirstName = firstName;
        user.LastName = lastName;
        user.PhoneNumber = phoneNumber;
        _context.SaveChanges();
    }
    
    /// <summary>
    ///     Gets all pending group invitations for a user.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>
    ///     A list of group invitations.
    /// </returns>
    [HttpGet(nameof(GetGroupInvitations)+"{userId}")]
    public IEnumerable<Invite> GetGroupInvitations(int userId)
    {
        return _context.Invites.Where(i => i.ReceiverId == userId).ToList();
    }
    
    /// <summary>
    ///     Accepts a group invitation.
    ///     A user can only accept an invitation if:
    ///     - The user is not already in the group.
    ///     - The group is not closed.
    ///     - The group is not marked ToBePaid.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    [HttpPut(nameof(AcceptGroupInvitation)+"{userId}/{groupId}")]
    public void AcceptGroupInvitation(int userId, int groupId)
    {
        var invite = _context.Invites.FirstOrDefault(i => i.ReceiverId == userId && i.GroupId == groupId);
        _context.Invites.Remove(invite);
        
        if (_context.UserToGroups.Any(u => u.GroupId == groupId && u.UserId == userId))
            return;
        if (_context.Groups.FirstOrDefault(g => g.Id == groupId).Closed)
            return;
        if (_context.ToBePaids.Any(t => t.UserToGroupId == _context.UserToGroups.FirstOrDefault(u => u.GroupId == groupId).Id))
            return;
        
        _context.UserToGroups.Add(new UserToGroup
        {
            UserId = userId,
            GroupId = groupId,
            IsOwner = false,
        });
        _context.SaveChanges();
    }
    
    /// <summary>
    ///     Gets all the groups a user is in.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>
    ///     A list of groups.
    /// </returns>
    [HttpGet(nameof(GetGroups)+"{userId}")]
    public IEnumerable<Group> GetGroups(int userId)
    {
        return _context.Groups.Where(g => _context.UserToGroups.Any(u => u.GroupId == g.Id && u.UserId == userId)).ToList();
    }
}