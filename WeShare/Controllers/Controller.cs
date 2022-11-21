using Microsoft.AspNetCore.Mvc;
using WebAPI.Models;

namespace WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class Controller : ControllerBase
{
    private readonly WeshareContext _context = new();

    private int GetUserToGroupId(int userId, int groupId)
    {
        var userToGroup = (from utg in _context.UserToGroups
            where utg.UserId == userId && utg.GroupId == groupId
            select utg).FirstOrDefault();
        return userToGroup?.Id ?? 0;
    }

    private IEnumerable<int> GetUserToGroupIdsByGroupId(int groupId)
    {
        return from utg in _context.UserToGroups
            where utg.GroupId == groupId
            select utg.Id;
    }

    /// <summary>
    ///     Gets all user emails from the database.
    /// </summary>
    /// <returns>
    ///     A List of all user emails.
    /// </returns>
    [HttpGet(nameof(GetUserEmails))]
    public IEnumerable<string> GetUserEmails()
    {
        return from user in _context.Users
            select user.Email;
    }

    /// <summary>
    ///     Gets all deactivated user emails from the database.
    /// </summary>
    /// <returns>
    ///     A List of all deactivated user emails.
    /// </returns>
    [HttpGet(nameof(GetDeactivatedUserEmails))]
    public IEnumerable<string> GetDeactivatedUserEmails()
    {
        return from user in _context.Users
            where _context.DeactivatedUsers.Any(deactivatedUser => deactivatedUser.UserId == user.Id)
            select user.Email;
    }

    /// <summary>
    ///     Gets a user from the database.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>
    ///     A User object.
    /// </returns>
    [HttpGet(nameof(GetUser) + "{userId}")]
    public IEnumerable<User> GetUser(int userId)
    {
        return from user in _context.Users
            where user.Id == userId
            select user;
    }

    /// <summary>
    ///     Inserts a new user in the database.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="firstName"></param>
    /// <param name="lastName"></param>
    /// <param name="phoneNumber"></param>
    [HttpPost(nameof(InsertUser) + "{email}/{firstName}/{lastName}/{phoneNumber}")]
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
    ///     Updates a user's information.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="email"></param>
    /// <param name="firstName"></param>
    /// <param name="lastName"></param>
    /// <param name="phoneNumber"></param>
    [HttpPut(nameof(UpdateUser) + "{userId}/{email}/{firstName}/{lastName}/{phoneNumber}")]
    public void UpdateUser(int userId, string email, string firstName, string lastName, string phoneNumber)
    {
        var user = (from u in _context.Users
            where u.Id == userId
            select u).FirstOrDefault();
        if (user == null) return;
        user.Email = email;
        user.FirstName = firstName;
        user.LastName = lastName;
        user.PhoneNumber = phoneNumber;
        _context.SaveChanges();
    }

    /// <summary>
    ///     Gets all the groups a user is in.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>
    ///     A list of groups.
    /// </returns>
    [HttpGet(nameof(GetGroups) + "{userId}")]
    public IEnumerable<Group> GetGroups(int userId)
    {
        return from g in _context.Groups
            join utg in _context.UserToGroups
                on g.Id equals utg.GroupId
            where utg.UserId == userId
            select g;
    }

    /// <summary>
    ///     Creates a new group in the database.
    ///     Automatically adds the creator to the group as the owner.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupName"></param>
    [HttpPost(nameof(InsertGroup) + "{userId}/{groupName}")]
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
    ///     Gets all payments of a user in a group.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <returns>
    ///     A List of all payments of a user in a group.
    /// </returns>
    [HttpGet(nameof(GetUserPayments) + "{userId}/{groupId}")]
    public IEnumerable<Payment> GetUserPayments(int userId, int groupId)
    {
        var userToGroupId = GetUserToGroupId(userId, groupId);
        return from payment in _context.Payments
            where payment.UserToGroupId == userToGroupId
            select payment;
    }

    /// <summary>
    ///     Gets all the current payments for a group.
    /// </summary>
    /// <param name="groupId"></param>
    /// <returns></returns>
    [HttpGet(nameof(GetGroupPayments) + "{groupId}")]
    public IEnumerable<Payment> GetGroupPayments(int groupId)
    {
        return from payment in _context.Payments
            where GetUserToGroupIdsByGroupId(groupId).Contains(payment.UserToGroupId)
            select payment;
    }

    /// <summary>
    ///     Makes a payment for a user in a group.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <param name="amount"></param>
    [HttpPost(nameof(InsertPayment) + "{userId}/{groupId}/{amount}")]
    public void InsertPayment(int userId, int groupId, double amount)
    {
        var userToGroupId = GetUserToGroupId(userId, groupId);
        if (userToGroupId == 0) return;
        _context.Payments.Add(new Payment
        {
            UserToGroupId = userToGroupId,
            Amount = amount,
            Date = DateTime.Now
        });
        // Deduct from wallet
        var wallet = (from w in _context.Wallets
            where w.UserId == userId
            select w).FirstOrDefault();
		if (wallet == null) return;
		
		wallet.Balance -= amount;
        _context.SaveChanges();
    }

    /// <summary>
    ///     Gets all the friends of a user.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>
    ///     A List of all the friends of a user.
    /// </returns>
    [HttpGet(nameof(GetFriendships) + "{userId}")]
    public IEnumerable<Friendship> GetFriendships(int userId)
    {
        return from f in _context.Friendships
            where f.User1Id == userId
            select f;
    }

    /// <summary>
    ///     Inserts a new friendship in the database.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="friendId"></param>
    [HttpPost(nameof(InsertFriendship) + "{userId}/{friendId}")]
    public void InsertFriendship(int userId, int friendId)
    {
        var friendship = from f in _context.Friendships 
                         where f.User1Id == userId && f.User2Id == friendId 
                         select f;
		if (friendship.Any()) return;

		_context.Friendships.Add(new Friendship
        {
            User1Id = userId,
            User2Id = friendId
        });
        _context.SaveChanges();
    }


    /// <summary>
    ///     Removes a friendship from the database.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="friendId"></param>
    [HttpDelete(nameof(DeleteFriendship) + "{userId}/{friendId}")]
    public void DeleteFriendship(int userId, int friendId)
    {
        var friendship = (from f in _context.Friendships
            where f.User1Id == userId && f.User2Id == friendId
            select f).FirstOrDefault();
        if (friendship == null) return;
        _context.Friendships.Remove(friendship);
        _context.SaveChanges();
    }

    /// <summary>
    ///     Gets all pending group invitations for a user.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>
    ///     A list of group invitations.
    /// </returns>
    [HttpGet(nameof(GetInvites) + "{userId}")]
    public IEnumerable<Invite> GetInvites(int userId)
    {
        return from i in _context.Invites
            where i.ReceiverId == userId
            select i;
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
    [HttpPost(nameof(InsertInvite) + "{senderId}/{receiverId}/{groupId}")]
    public void InsertInvite(int senderId, int receiverId, int groupId)
    {
        if (_context.UserToGroups.Any(u => u.UserId == receiverId && u.GroupId == groupId))
            return;
        if (_context.Invites.Any(i => i.SenderId == senderId && i.ReceiverId == receiverId && i.GroupId == groupId))
            return;
        if (_context.DeactivatedUsers.Any(u => u.UserId == receiverId))
            return;
        if (_context.Groups.Any(g => g.Id == groupId && g.Closed))
            return;
        if (_context.ToBePaids.Any(t => GetUserToGroupIdsByGroupId(groupId).Contains(t.UserToGroupId)))
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
    ///     Accepts a group invitation.
    ///     A user can only accept an invitation if:
    ///     - The user is not already in the group.
    ///     - The group is not closed.
    ///     - The group is not marked ToBePaid.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    [HttpPut(nameof(AcceptInvite) + "{userId}/{groupId}")]
    public void AcceptInvite(int userId, int groupId)
    {
        var invite = (from i in _context.Invites
            where i.ReceiverId == userId && i.GroupId == groupId
            select i).FirstOrDefault();
        if (invite == null) return;
        _context.Invites.Remove(invite);

        if (GetUserToGroupId(userId, groupId) != 0) return;
        if (_context.Groups.Any(g => g.Id == groupId && g.Closed)) return;
        if (_context.ToBePaids.Any(tbp => GetUserToGroupIdsByGroupId(groupId).Contains(tbp.UserToGroupId))) return;

        _context.UserToGroups.Add(new UserToGroup
        {
            UserId = userId,
            GroupId = groupId,
            IsOwner = false
        });
        _context.SaveChanges();
    }

    /// <summary>
    ///     Removes a group invite from the database.
    /// </summary>
    /// <param name="receiverId"></param>
    /// <param name="groupId"></param>
    [HttpDelete(nameof(DeleteInvite) + "{receiverId}/{groupId}")]
    public void DeleteInvite(int receiverId, int groupId)
    {
        var invite = (from i in _context.Invites
            where i.ReceiverId == receiverId && i.GroupId == groupId
            select i).First();
        if (invite == null) return;
        _context.Invites.Remove(invite);
        _context.SaveChanges();
    }

    /// <summary>
    ///     Gets the to be paids for a group.
    /// </summary>
    /// <param name="groupId"></param>
    /// <returns>
    ///     A list of to be paids.
    /// </returns>
    [HttpGet(nameof(GetGroupToBePaids) + "{groupId}")]
    public IEnumerable<ToBePaid> GetGroupToBePaids(int groupId)
    {
        return from tbp in _context.ToBePaids
            where GetUserToGroupIdsByGroupId(groupId).Contains(tbp.UserToGroupId)
            select tbp;
    }

    /// <summary>
    ///     Get the to be paid for a user in a group.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <returns>
    ///     A to be paid.
    /// </returns>
    [HttpGet(nameof(GetUserToBePaid) + "{userId}/{groupId}")]
    public IEnumerable<ToBePaid> GetUserToBePaid(int userId, int groupId)
    {
        var userToGroupId = GetUserToGroupId(userId, groupId);
        return from tbp in _context.ToBePaids
            where tbp.UserToGroupId == userToGroupId
            select tbp;
    }

    /// <summary>
    ///     Inserts new values to ToBePaid table in the database.
    /// </summary>
    /// <param name="groupId"></param>
    /// <param name="userId"></param>
    [HttpPost(nameof(InsertToBePaid) + "{groupId}/{userId}")]
    public void InsertToBePaid(int groupId, int userId)
    {
        if (!_context.UserToGroups.Any(u => u.UserId == userId && u.GroupId == groupId && u.IsOwner))
            return;
        if (_context.ToBePaids.Any(t => t.UserToGroupId == _context.UserToGroups.First(u => u.UserId == userId && u.GroupId == groupId).Id))
            return;
        foreach (var userToGroupId in _context.UserToGroups.Where(u => u.GroupId == groupId).Select(u => u.Id))
            _context.ToBePaids.Add(new ToBePaid
            {
                UserToGroupId = userToGroupId,
                Approved = false,
                Date = null
            });

        _context.SaveChanges();

        // Remove all the invites for the group.
        foreach (var invite in _context.Invites.Where(i => i.GroupId == groupId))
            DeleteInvite(invite.ReceiverId, invite.GroupId);
    }

    /// <summary>
    ///     Approves a ToBePaid value in the database.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    [HttpPut(nameof(ApproveToBePaid) + "{userId}/{groupId}")]
    public void ApproveToBePaid(int groupId, int userId)
    {
        var userToGroupId = GetUserToGroupId(userId, groupId);
        if (userToGroupId == 0) return;

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
    ///     Removes a group's ToBePaid values from the database.
    /// </summary>
    /// <param name="groupId"></param>
    /// <param name="userId"></param>
    [HttpDelete(nameof(DeleteToBePaid) + "{groupId}/{userId}")]
    public void DeleteToBePaid(int groupId, int userId)
    {
        var userToGroupId = GetUserToGroupId(userId, groupId);
        if (userToGroupId == 0) return;

        // If the user is not the owner of the group, return.
        if (!_context.UserToGroups.Any(u => u.UserId == userId && u.GroupId == groupId && u.IsOwner)) return;
        // If all ToBePaid values are approved for the group, return.
        var userToGroupIds = GetUserToGroupIdsByGroupId(groupId);
        if (_context.ToBePaids.Where(t => userToGroupIds.Contains(t.UserToGroupId)).All(t => t.Approved)) return;

        userToGroupIds.ToList().ForEach(utg =>
            _context.ToBePaids.Remove(_context.ToBePaids.First(t => t.UserToGroupId == utg)));
        _context.SaveChanges();
    }


    /// <summary>
    ///     Inserts receipts for a group.
    /// </summary>
    /// <param name="groupId"></param>
    private void InsertReceipts(int groupId)
    {
        var userToGroupIds = GetUserToGroupIdsByGroupId(groupId);
        // Get the total amount of money that the group has spent.
        var totalAmount = _context.Payments.Where(p => userToGroupIds.Contains(p.UserToGroupId)).Sum(p => p.Amount);
        // Get the amount that each user should pay.
        var amountPerUser = totalAmount / userToGroupIds.Count();
        // Get every payment for each user even if the user didn't pay anything.
        var payments = _context.Payments.Where(p => userToGroupIds.Contains(p.UserToGroupId)).ToList();
        // If payments does not contain a payment for a user, add it with amount 0.
        foreach (var userToGroupId in userToGroupIds)
        {
            if (payments.Any(p => p.UserToGroupId == userToGroupId)) continue;
            payments.Add(new Payment
            {
                Amount = 0,
                UserToGroupId = userToGroupId
            });
        }

        payments.ForEach(
            p => _context.Receipts.Add(new Receipt
            {
                Amount = p.Amount - amountPerUser,
                UserToGroupId = p.UserToGroupId,
                Fulfilled = false
            }));

        _context.SaveChanges();
    }

    /// <summary>
    ///     Gets the receipt for a user in a group.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <returns>
    ///     The receipt for a user in a group.
    /// </returns>
    [HttpGet(nameof(GetUserGroupReceipt) + "{userId}/{groupId}")]
    public IEnumerable<Receipt> GetUserGroupReceipt(int userId, int groupId)
    {
        var userToGroupId = GetUserToGroupId(userId, groupId);
        return from receipt in _context.Receipts
            where receipt.UserToGroupId == userToGroupId
            select receipt;
    }

    /// <summary>
    ///     Pays a receipt by deducting the amount from the user's balance and marking the receipt as fulfilled.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    [HttpPut(nameof(FulfillReceipt) + "{userId}/{groupId}")]
    public void FulfillReceipt(int groupId, int userId)
    {
        var userToGroupId = GetUserToGroupId(userId, groupId);
        if (userToGroupId == 0) return;

        var receipt = (from r in _context.Receipts
            where r.UserToGroupId == userToGroupId
            select r).FirstOrDefault();
        if (receipt == null) return;

        var wallet = (from w in _context.Wallets
            where w.UserId == userId
            select w).FirstOrDefault();
        if (wallet == null) return;

        if (wallet.Balance < receipt.Amount) return;
        // += because if someone owes money, the amount is negative.
        wallet.Balance += receipt.Amount;
        receipt.Date = DateTime.Now;
        receipt.Fulfilled = true;
        _context.SaveChanges();

        // if all receipts are fulfilled for the group, close the group.
        var groupReceipts = from r in _context.Receipts
            where r.UserToGroupId == userToGroupId
            select r;
        if (!groupReceipts.All(r => r.Fulfilled)) return;

        var group = (from g in _context.Groups
            where g.Id == groupId
            select g).First();
        group.Closed = true;
        _context.SaveChanges();
    }

    /// <summary>
    ///     Removes a user from a group.
    /// </summary>
    /// <param name="groupId"></param>
    /// <param name="userId"></param>
    /// <param name="userToRemoveId"></param>
    [HttpDelete(nameof(DeleteUserFromGroup) + "{groupId}/{userId}/{userToRemoveId}")]
    public void DeleteUserFromGroup(int groupId, int userId, int userToRemoveId)
    {
        var userToGroupId = GetUserToGroupId(userId, groupId);
        if (userToGroupId == 0) return;

        if (_context.Receipts.Any(r => r.UserToGroupId == userToGroupId) || // if the user has a receipt in the group
            _context.Payments.Any(p => p.UserToGroupId == userToGroupId) || // if the user has a payment in the group
            _context.ToBePaids.Any(t => t.UserToGroupId == userToGroupId) || // if the user has a ToBePaid in the group
            !_context.UserToGroups.Any(utg =>
                utg.GroupId == groupId && utg.UserId == userId &&
                utg.IsOwner) || // if the user who wants to remove someone is not the owner
            _context.Groups.Any(g => g.Id == groupId && g.Closed)) // if the group is closed
            return;

        var userToRemove =
            _context.UserToGroups.FirstOrDefault(u => u.UserId == userToRemoveId && u.GroupId == groupId);
        if (userToRemove == null) return;
        _context.UserToGroups.Remove(userToRemove);
        _context.SaveChanges();
    }


    /// <summary>
    ///     Creates a wallet of a random amount of money for a user.
    /// </summary>
    /// <param name="userId"></param>
    [HttpPost(nameof(InsertWallet) + "{userId}")]
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
    [HttpPut(nameof(DeactivateUser) + "{userId}/{until}")]
    public void DeactivateUser(int userId, DateTime until)
    {
        if (_context.DeactivatedUsers.Any(du => du.UserId == userId)) return;
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
    [HttpPut(nameof(ActivateUser) + "{userId}")]
    public void ActivateUser(int userId)
    {
        var user = (from u in _context.DeactivatedUsers
            where u.UserId == userId
            select u).FirstOrDefault();
        if (user == null) return;
        _context.DeactivatedUsers.Remove(user);
        _context.SaveChanges();
    }
}