using LWSAuthService.Models;
using LWSAuthService.Models.Event;
using LWSAuthService.Models.Inner;
using LWSAuthService.Models.Request;
using LWSAuthService.Repository;

namespace LWSAuthService.Service;

public class AccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IEventRepository _eventRepository;

    public AccountService(IAccountRepository accountRepository, IEventRepository eventRepository)
    {
        _accountRepository = accountRepository;
        _eventRepository = eventRepository;
    }

    public async Task<InternalCommunication<object>> CreateNewAccount(RegisterRequest registerRequest)
    {
        var prevAccount = await _accountRepository.GetAccountByEmailAsync(registerRequest.UserEmail);
        if (prevAccount != null)
        {
            return new InternalCommunication<object>
            {
                ResultType = ResultType.DataConflicts,
                Message = $"User email {registerRequest.UserEmail} already registered in server!"
            };
        }

        var createdAccount = await _accountRepository.CreateAccountAsync(registerRequest.ToUserAccount());
        await _eventRepository.SendMessageToTopicAsync("account.created", new AccountCreatedMessage
        {
            AccountId = createdAccount.Id,
            CreatedAt = DateTimeOffset.Now
        });

        return new InternalCommunication<object> {ResultType = ResultType.Success};
    }

    public async Task<InternalCommunication<Account>> LoginAccount(LoginRequest loginRequest)
    {
        var account = await _accountRepository.GetAccountByEmailAsync(loginRequest.UserEmail);
        if (account == null)
        {
            return new InternalCommunication<Account>
            {
                ResultType = ResultType.DataNotFound,
                Message = "Login failed! Please check email or id."
            };
        }

        if (!CheckPasswordCorrect(loginRequest.UserPassword, account.UserPassword))
        {
            return new InternalCommunication<Account>
            {
                ResultType = ResultType.DataNotFound,
                Message = "Login failed! Please check email or id."
            };
        }

        return new InternalCommunication<Account>
        {
            Result = account,
            ResultType = ResultType.Success
        };
    }

    public async Task<InternalCommunication<Account>> GetAccountInfoAsync(string userId)
    {
        var account = await _accountRepository.GetAccountByIdAsync(userId);
        if (account == null)
        {
            return new InternalCommunication<Account>
            {
                ResultType = ResultType.DataNotFound,
                Message = "Cannot find data corresponding userId!"
            };
        }

        return new InternalCommunication<Account>
        {
            ResultType = ResultType.Success,
            Result = account
        };
    }

    public async Task<InternalCommunication<object>> RemoveAccountAsync(string userId)
    {
        var account = await _accountRepository.GetAccountByIdAsync(userId);
        if (account == null)
        {
            return new InternalCommunication<object>
            {
                ResultType = ResultType.DataNotFound,
                Message = "Cannot find data corresponding userId!"
            };
        }

        await _accountRepository.RemoveAccountAsync(account);

        return new InternalCommunication<object>
        {
            ResultType = ResultType.Success
        };
    }

    private bool CheckPasswordCorrect(string plainPassword, string hashedPassword)
    {
        bool correct = false;
        try
        {
            correct = BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
        }
        catch
        {
        }

        return correct;
    }
}