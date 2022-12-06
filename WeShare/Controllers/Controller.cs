using Microsoft.AspNetCore.Mvc;
using WebAPI.Models;
using Action = WebAPI.Models.Action;

namespace WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class Controller : ControllerBase
{
    private readonly DbWeshareContext _context = new();

    /// <summary>
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <returns></returns>
    private int GetUserToGroupId(int userId, int groupId)
    {
        var userToGroup = (from utg in _context.UserToGroups
            where utg.UserId == userId && utg.GroupId == groupId
            select utg).FirstOrDefault();
        return userToGroup?.Id ?? 0;
    }

    /// <summary>
    /// </summary>
    /// <param name="groupId"></param>
    /// <returns></returns>
    private IEnumerable<int> GetUserToGroupIdsByGroupId(int groupId)
    {
        return from utg in _context.UserToGroups
            where utg.GroupId == groupId
            select utg.Id;
    }

    /// <summary>
    ///     Gets an id of an user by their email
    /// </summary>
    /// <param name="email"></param>
    /// <returns>
    ///     The id of the user.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetUserId) + "/{email}")]
    public IEnumerable<int> GetUserId(string email)
    {
        return from u in _context.Users
            where u.Email == email
            select u.Id;
    }

    /// <summary>
    ///     Gets the name of a user by their userToGroupId
    /// </summary>
    /// <param name="userToGroupId"></param>
    /// <returns>
    ///     The name of the user.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetUserNameByUserToGroupId) + "/{userToGroupId}")]
    public IEnumerable<string> GetUserNameByUserToGroupId(int userToGroupId)
    {
        return from utg in _context.UserToGroups
            join u in _context.Users on utg.UserId equals u.Id
            where utg.Id == userToGroupId
            select $"{u.FirstName} {u.LastName}";
    }

    /// <summary>
    ///     Checks if the user exists.
    /// </summary>
    /// <param name="userId" />
    /// <returns>
    ///     A bool value representing if the user exists.
    /// </returns>
    [HttpGet]
    [Route(nameof(IsUserExists) + "/{userId}")]
    public IEnumerable<bool> IsUserExists(int userId)
    {
        yield return (from u in _context.Users
            where u.Id == userId
            select u).FirstOrDefault() is not null;
    }

    /// <summary>
    ///     Gets if a user is the owner of a group.
    /// </summary>
    /// <param name="userId" />
    /// <param name="groupId" />
    /// <returns>
    ///     A bool value representing if the user is the owner of the group.
    /// </returns>
    [HttpGet]
    [Route(nameof(IsUserOwner) + "/{userId}/{groupId}")]
    public IEnumerable<bool> IsUserOwner(int userId, int groupId)
    {
        yield return (from g in _context.Groups
            join utg in _context.UserToGroups on g.Id equals utg.GroupId
            where g.Id == groupId && utg.UserId == userId && utg.IsOwner
            select g).FirstOrDefault() is not null;
    }

    /// <summary>
    ///     Checks if the user is deactivated.
    /// </summary>
    /// <param name="userId" />
    /// <returns>
    ///     A bool value representing if the user is deactivated
    /// </returns>
    [HttpGet]
    [Route(nameof(IsUserDeactivated) + "/{userId}")]
    public IEnumerable<bool> IsUserDeactivated(int userId)
    {
        yield return (from u in _context.DeactivatedUsers
            where u.Id == userId
            select u).FirstOrDefault() is not null;
    }

    /// <summary>
    ///     Gets a user from the database.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>
    ///     A User object.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetUser) + "/{userId}")]
    public IEnumerable<User> GetUser(int userId)
    {
        return from user in _context.Users
            where user.Id == userId
            select user;
    }

    /// <summary>
    ///     Gets all the users in a group.
    /// </summary>
    /// <param name="groupId"></param>
    /// <returns>
    ///     A list of users.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetUsers) + "/{groupId}")]
    public IEnumerable<User> GetUsers(int groupId)
    {
        return from u in _context.Users
            join utg in _context.UserToGroups
                on u.Id equals utg.UserId
            where utg.GroupId == groupId
            select u;
    }

    /// <summary>
    ///     Inserts a new user in the database.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="firstName"></param>
    /// <param name="lastName"></param>
    /// <param name="phoneNumber"></param>
    [HttpPost]
    [Route(nameof(InsertUser) + "/{email}/{firstName}/{lastName}/{phoneNumber}")]
    public void InsertUser(string email, string firstName, string lastName, string phoneNumber)
    {
        var user = (from u in _context.Users
            where u.Email == email || u.PhoneNumber == phoneNumber
            select u).FirstOrDefault();
        if (user is not null)
            throw new Exception($"User with email {email} or phone number {phoneNumber} already exists.");

        _context.Users.Add(new User
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber
        });
        _context.SaveChanges();

        InsertWallet(GetUserId(email).First());
    }

    /// <summary>
    ///     Inserts a wallet of a random amount of money for a user.
    /// </summary>
    /// <param name="userId"></param>
    private void InsertWallet(int userId)
    {
        _context.Wallets.Add(new Wallet
        {
            UserId = userId,
            Balance = new Random().Next(25000, 99999)
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
    [HttpPut]
    [Route(nameof(UpdateUser) + "/{userId}/{email}/{firstName}/{lastName}/{phoneNumber}")]
    public void UpdateUser(int userId, string email, string firstName, string lastName, string phoneNumber)
    {
        var user = (from u in _context.Users
            where u.Id == userId
            select u).FirstOrDefault();
        if (user is null) throw new Exception($"User with id {userId} does not exist.");

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
    [HttpGet]
    [Route(nameof(GetGroups) + "/{userId}")]
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
    [HttpPost]
    [Route(nameof(InsertGroup) + "/{userId}/{groupName}")]
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
    [HttpGet]
    [Route(nameof(GetUserPayments) + "/{userId}/{groupId}")]
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
    [HttpGet]
    [Route(nameof(GetGroupPayments) + "/{groupId}")]
    public IEnumerable<Payment> GetGroupPayments(int groupId)
    {
        return from payment in _context.Payments
            where GetUserToGroupIdsByGroupId(groupId).Contains(payment.UserToGroupId)
            select payment;
    }

    /// <summary>
    ///     Inserts a payment for a user in a group.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <param name="title"></param>
    /// <param name="amount"></param>
    [HttpPost]
    [Route(nameof(InsertPayment) + "/{userId}/{groupId}/{title}/{amount}")]
    public void InsertPayment(int userId, int groupId, string title, double amount)
    {
        var userToGroupId = GetUserToGroupId(userId, groupId);
        if (userToGroupId == 0)
            throw new Exception($"User {userId} is not in group {groupId}.");

        _context.Payments.Add(new Payment
        {
            UserToGroupId = userToGroupId,
            Title = title,
            Amount = amount,
            Date = DateTime.Now
        });
        // Deduct from wallet
        var wallet = (from w in _context.Wallets
            where w.UserId == userId
            select w).FirstOrDefault();

        if (wallet is null)
            throw new Exception($"No wallet found for user {userId}");
        if (wallet.Balance < amount)
            throw new Exception($"Not enough money in wallet for user {userId}");

        wallet.Balance -= amount;
        _context.SaveChanges();
    }

    /// <summary>
    ///     Gets all the friendships of a user.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>
    ///     A List of all the friends of a user.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetFriendships) + "/{userId}")]
    public IEnumerable<Friendship> GetFriendships(int userId)
    {
        return from f in _context.Friendships
            where f.UserId == userId
            select f;
    }

    /// <summary>
    ///     Inserts a new friendship in the database.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="friendId"></param>
    [HttpPost]
    [Route(nameof(InsertFriendship) + "/{userId}/{friendId}")]
    public void InsertFriendship(int userId, int friendId)
    {
        var friendship = from f in _context.Friendships
            where f.UserId == userId && f.FriendId == friendId
            select f;
        if (friendship.Any())
            throw new Exception($"Friendship between user {userId} and user {friendId} already exists.");

        _context.Friendships.Add(new Friendship
        {
            UserId = userId,
            FriendId = friendId
        });
        _context.SaveChanges();
    }


    /// <summary>
    ///     Removes a friendship from the database.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="friendId"></param>
    [HttpDelete]
    [Route(nameof(DeleteFriendship) + "/{userId}/{friendId}")]
    public void DeleteFriendship(int userId, int friendId)
    {
        var friendship = (from f in _context.Friendships
            where f.UserId == userId && f.FriendId == friendId
            select f).FirstOrDefault();
        if (friendship is null)
            throw new Exception($"Friendship between user {userId} and user {friendId} does not exist.");

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
    [HttpGet]
    [Route(nameof(GetInvites) + "/{userId}")]
    public IEnumerable<Invite> GetInvites(int userId)
    {
        return from i in _context.Invites
            where i.ReceiverId == userId
            select i;
    }
    
    /// <summary>
    ///     Returns a list of all invitable friends for a user in a group.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    /// <returns>
    ///     A list of all invitable friends for a user in a group.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetInvitableFriends) + "/{userId}/{groupId}")]
    public IEnumerable<User> GetInvitableFriends(int userId, int groupId)
    {
        var invitableFriends = from u in _context.Users 
            join f in _context.Friendships on u.Id equals f.FriendId 
            where f.UserId == userId
            join utg in _context.UserToGroups on u.Id equals utg.UserId into utgJoin
            from utg in utgJoin.DefaultIfEmpty()
            where utg == null || utg.GroupId != groupId
            select u;
        return invitableFriends;
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
    [HttpPost]
    [Route(nameof(InsertInvite) + "/{senderId}/{receiverId}/{groupId}")]
    public void InsertInvite(int senderId, int receiverId, int groupId)
    {
        if (_context.UserToGroups.Any(u => u.UserId == receiverId && u.GroupId == groupId))
            throw new Exception($"User {receiverId} is already in group {groupId}.");
        if (_context.Invites.Any(i => i.SenderId == senderId && i.ReceiverId == receiverId && i.GroupId == groupId))
            throw new Exception($"User {receiverId} has already been invited to group {groupId}.");
        if (_context.DeactivatedUsers.Any(u => u.UserId == receiverId))
            throw new Exception($"User {receiverId} is deactivated.");
        if (_context.Groups.Any(g => g.Id == groupId && g.Closed))
            throw new Exception($"Group {groupId} is closed.");
        if (_context.ToBePaids.Any(t => GetUserToGroupIdsByGroupId(groupId).Contains(t.UserToGroupId)))
            throw new Exception($"Group {groupId} has a value in the ToBePaid table.");
        if (!_context.UserToGroups.Any(u => u.UserId == senderId && u.GroupId == groupId && u.IsOwner))
            throw new Exception($"User {senderId} is not the owner of group {groupId}.");

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
    ///     - The user was invited to the group.
    ///     - The user is not already in the group.
    ///     - The group is not closed.
    ///     - The group is not marked ToBePaid.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    [HttpPut]
    [Route(nameof(AcceptInvite) + "/{userId}/{groupId}")]
    public void AcceptInvite(int userId, int groupId)
    {
        var invite = (from i in _context.Invites
            where i.ReceiverId == userId && i.GroupId == groupId
            select i).FirstOrDefault();
        if (invite is null)
            throw new Exception($"User {userId} has not been invited to group {groupId}.");
        _context.Invites.Remove(invite);

        if (GetUserToGroupId(userId, groupId) is not 0)
            throw new Exception($"User {userId} is already in group {groupId}.");
        if (_context.Groups.Any(g => g.Id == groupId && g.Closed))
            throw new Exception($"Group {groupId} is closed.");
        if (_context.ToBePaids.Any(tbp => GetUserToGroupIdsByGroupId(groupId).Contains(tbp.UserToGroupId)))
            throw new Exception($"Group {groupId} has a value in the ToBePaid table.");

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
    [HttpDelete]
    [Route(nameof(DeleteInvite) + "/{receiverId}/{groupId}")]
    public void DeleteInvite(int receiverId, int groupId)
    {
        var invite = (from i in _context.Invites
            where i.ReceiverId == receiverId && i.GroupId == groupId
            select i).FirstOrDefault();
        if (invite is null) throw new Exception($"Invite for user {receiverId} to group {groupId} does not exist.");

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
    [HttpGet]
    [Route(nameof(GetGroupToBePaids) + "/{groupId}")]
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
    [HttpGet]
    [Route(nameof(GetUserToBePaid) + "/{userId}/{groupId}")]
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
    [HttpPost]
    [Route(nameof(InsertToBePaid) + "/{groupId}/{userId}")]
    public void InsertToBePaid(int groupId, int userId)
    {
        var userToGroupIds = GetUserToGroupIdsByGroupId(groupId);

        var isOwner = from utg in _context.UserToGroups
            where utg.UserId == userId && utg.GroupId == groupId
            select utg.IsOwner;
        if (!isOwner.First()) throw new Exception($"User {userId} is not the owner of group {groupId}.");

        var isToBePaid = from tbp in _context.ToBePaids
            where userToGroupIds.Contains(tbp.UserToGroupId)
            select tbp;
        if (isToBePaid.Any()) throw new Exception($"Group {groupId} is already marked ToBePaid.");

        foreach (var userToGroupId in userToGroupIds)
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
    [HttpPut]
    [Route(nameof(ApproveToBePaid) + "/{userId}/{groupId}")]
    public void ApproveToBePaid(int groupId, int userId)
    {
        var userToGroupId = GetUserToGroupId(userId, groupId);
        if (userToGroupId == 0)
            throw new Exception($"User {userId} is not in group {groupId}.");

        var toBePaid = _context.ToBePaids.FirstOrDefault(t => t.UserToGroupId == userToGroupId);
        if (toBePaid is null)
            throw new Exception($"User {userId} is not marked ToBePaid in group {groupId}.");

        if (toBePaid.Approved)
            throw new Exception($"User {userId} already approved ToBePaid in group {groupId}.");

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
    [HttpDelete]
    [Route(nameof(DeleteToBePaid) + "/{groupId}/{userId}")]
    public void DeleteToBePaid(int groupId, int userId)
    {
        var userToGroupId = GetUserToGroupId(userId, groupId);
        if (userToGroupId == 0)
            throw new Exception($"User {userId} is not in group {groupId}.");

        // If the user is not the owner of the group
        if (!_context.UserToGroups.Any(u => u.UserId == userId && u.GroupId == groupId && u.IsOwner))
            throw new Exception($"User {userId} is not the owner of group {groupId}.");

        // If all ToBePaid values are approved for the group
        var userToGroupIds = GetUserToGroupIdsByGroupId(groupId);
        if (_context.ToBePaids.Where(t => userToGroupIds.Contains(t.UserToGroupId)).All(t => t.Approved))
            throw new Exception($"Group {groupId} is already approved ToBePaid.");

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
        var userToGroupIds = GetUserToGroupIdsByGroupId(groupId).ToList();
        // Get the total amount of money that the group has spent.
        var totalAmount = _context.Payments.Where(p => userToGroupIds.Contains(p.UserToGroupId)).Sum(p => p.Amount);
        // Get the amount that each user should pay.
        var amountPerUser = totalAmount / userToGroupIds.Count;
        // Get every payment for each user even if the user didn't pay anything.
        var payments = _context.Payments.Where(p => userToGroupIds.Contains(p.UserToGroupId)).ToList();
        // If payments does not contain a payment for a user, add it with amount 0.
        foreach (var userToGroupId in userToGroupIds.Where(userToGroupId =>
                     payments.All(p => p.UserToGroupId != userToGroupId)))
            payments.Add(new Payment
            {
                Amount = 0,
                UserToGroupId = userToGroupId
            });

        // Get the amount that each user has paid.
        foreach (var utg in userToGroupIds)
        {
            var totalPaid = payments.Where(p => p.UserToGroupId == utg).Sum(p => p.Amount);
            _context.Receipts.Add(new Receipt
            {
                UserToGroupId = utg,
                Amount = totalPaid - amountPerUser,
                Fulfilled = false
            });
        }

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
    [HttpGet]
    [Route(nameof(GetReceipt) + "/{userId}/{groupId}")]
    public IEnumerable<Receipt> GetReceipt(int userId, int groupId)
    {
        var userToGroupId = GetUserToGroupId(userId, groupId);
        return from receipt in _context.Receipts
            where receipt.UserToGroupId == userToGroupId
            select receipt;
    }

    /// <summary>
    ///     Fulfills a receipt by deducting the amount from the user's balance and marking the receipt as fulfilled.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    [HttpPut]
    [Route(nameof(FulfillReceipt) + "/{userId}/{groupId}")]
    public void FulfillReceipt(int groupId, int userId)
    {
        var userToGroupId = GetUserToGroupId(userId, groupId);
        if (userToGroupId is 0)
            throw new Exception($"User {userId} is not in group {groupId}.");

        var receipt = (from r in _context.Receipts
            where r.UserToGroupId == userToGroupId
            select r).FirstOrDefault();
        if (receipt is null)
            throw new Exception($"User {userId} does not have a receipt in group {groupId}.");

        var wallet = (from w in _context.Wallets
            where w.UserId == userId
            select w).FirstOrDefault();
        if (wallet is null)
            throw new Exception($"User {userId} does not have a wallet.");

        if (wallet.Balance < receipt.Amount)
            throw new Exception($"User {userId} does not have enough money in their wallet.");

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
    [HttpDelete]
    [Route(nameof(DeleteUserFromGroup) + "/{groupId}/{userId}/{userToRemoveId}")]
    public void DeleteUserFromGroup(int groupId, int userId, int userToRemoveId)
    {
        var userToGroupId = GetUserToGroupId(userToRemoveId, groupId);
        if (userToGroupId is 0)
            throw new Exception($"User {userToRemoveId} is not in group {groupId}.");
        
        if (_context.Groups.Any(g => g.Id == groupId && g.Closed))
            throw new Exception($"Cannot remove user {userToRemoveId} from group {groupId} because the group is closed.");

        if (!_context.UserToGroups.Any(utg => utg.UserId == userId && utg.GroupId == groupId && utg.IsOwner))
            throw new Exception($"Cannot remove user {userToRemoveId} from group {groupId} because user {userId} is not the owner.");
        
        if (_context.Receipts.Any(r => r.UserToGroupId == userToGroupId))
            throw new Exception($"Cannot remove user {userToRemoveId} from group {groupId} because they have a receipt.");
       
        if (_context.ToBePaids.Any(t => t.UserToGroupId == userToGroupId))
            throw new Exception($"Cannot remove user {userToRemoveId} from group {groupId} because the group is marked as to be paid.");
        
        if (_context.Payments.Any(p => p.UserToGroupId == userToGroupId))
            throw new Exception($"Cannot remove user {userToRemoveId} from group {groupId} because they have a payment.");

        var userToRemove = from utg in _context.UserToGroups
            where utg.Id == userToGroupId
            select utg;
        _context.UserToGroups.Remove(userToRemove.First());
        _context.SaveChanges();
    }


    /// <summary>
    ///     Gets the balance of a user.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>
    ///     The balance of a user.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetBalance) + "/{userId}")]
    public IEnumerable<double> GetBalance(int userId)
    {
        return from w in _context.Wallets
            where w.UserId == userId
            select w.Balance;
    }

    /// <summary>
    ///     Deactivates a user until a given date.
    /// </summary>
    /// <param name="userId"></param>
    [HttpPut]
    [Route(nameof(DeactivateUser) + "/{userId}")]
    public void DeactivateUser(int userId)
    {
        if (_context.DeactivatedUsers.Any(du => du.UserId == userId))
            throw new Exception($"User {userId} is already deactivated.");

        _context.DeactivatedUsers.Add(new DeactivatedUser
        {
            UserId = userId,
            ByAdmin = false
        });
        _context.SaveChanges();
    }

    /// <summary>
    ///     Activates a user.
    /// </summary>
    /// <param name="userId"></param>
    [HttpPut]
    [Route(nameof(ActivateUser) + "/{userId}")]
    public void ActivateUser(int userId)
    {
        var user = (from u in _context.DeactivatedUsers
            where u.UserId == userId
            select u).FirstOrDefault();
        if (user is null)
            throw new Exception($"User {userId} is not deactivated.");

        if (user.ByAdmin)
            throw new Exception($"User {userId} is deactivated by an admin and cannot activate themselves.");

        _context.DeactivatedUsers.Remove(user);
        _context.SaveChanges();
    }

    /// <summary>
    ///     Deactivates a user by an admin.
    /// </summary>
    /// <param name="adminId"></param>
    /// <param name="userId"></param>
    [HttpPut]
    [Route(nameof(AdminDeactivateUser) + "/{adminId}/{userId}")]
    public void AdminDeactivateUser(int adminId, int userId)
    {
        if (_context.DeactivatedUsers.Any(du => du.UserId == userId))
            throw new Exception($"User {userId} is already deactivated.");

        _context.DeactivatedUsers.Add(new DeactivatedUser
        {
            ByAdmin = true,
            UserId = userId
        });

        InsertAction("Put", $"Deactivated user {userId}", adminId);

        _context.SaveChanges();
    }

    /// <summary>
    ///     Activates a user by an admin.
    /// </summary>
    /// <param name="adminId"></param>
    /// <param name="userId"></param>
    [HttpPut]
    [Route(nameof(AdminActivateUser) + "/{adminId}/{userId}")]
    public void AdminActivateUser(int adminId, int userId)
    {
        var user = (from u in _context.DeactivatedUsers
            where u.UserId == userId
            select u).FirstOrDefault();
        if (user is null)
            throw new Exception($"User {userId} is not deactivated.");

        _context.DeactivatedUsers.Remove(user);

        InsertAction("Put", $"Activated user {userId}", adminId);

        _context.SaveChanges();
    }

    /// <summary>
    ///     Inserts an action made by an admin.
    /// </summary>
    /// <param name="actionType"></param>
    /// <param name="description"></param>
    /// <param name="adminId"></param>
    [HttpPost]
    [Route(nameof(InsertAction) + "/{actionType}/{description}/{adminId}")]
    public void InsertAction(string actionType, string description, int adminId)
    {
        _context.Actions.Add(new Action
        {
            ActionType = actionType,
            Description = description,
            AdminId = adminId,
            Date = DateTime.Now
        });
        _context.SaveChanges();
    }

    /// <summary>
    ///     Gets all actions made by an admin.
    /// </summary>
    /// <returns>
    ///     All actions made by an admin.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetActions) + "/{adminId}")]
    public IEnumerable<Action> GetActions(int adminId)
    {
        return from a in _context.Actions
            where a.AdminId == adminId
            select a;
    }
}