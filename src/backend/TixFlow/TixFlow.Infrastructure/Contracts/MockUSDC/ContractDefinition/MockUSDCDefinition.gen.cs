using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Numerics;
using Nethereum.Hex.HexTypes;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Contracts.CQS;
using Nethereum.Contracts;
using System.Threading;

namespace TixFlow.Infrastructure.Contracts.MockUSDC.ContractDefinition
{


    public partial class MockUSDCDeployment : MockUSDCDeploymentBase
    {
        public MockUSDCDeployment() : base(BYTECODE) { }
        public MockUSDCDeployment(string byteCode) : base(byteCode) { }
    }

    public class MockUSDCDeploymentBase : ContractDeploymentMessage
    {
        public static string BYTECODE = "0x";
        public MockUSDCDeploymentBase() : base(BYTECODE) { }
        public MockUSDCDeploymentBase(string byteCode) : base(byteCode) { }

    }

    public partial class DomainSeparatorFunction : DomainSeparatorFunctionBase { }

    [Function("DOMAIN_SEPARATOR", "bytes32")]
    public class DomainSeparatorFunctionBase : FunctionMessage
    {

    }

    public partial class FaucetLimitFunction : FaucetLimitFunctionBase { }

    [Function("FAUCET_LIMIT", "uint256")]
    public class FaucetLimitFunctionBase : FunctionMessage
    {

    }

    public partial class PermitTypehashFunction : PermitTypehashFunctionBase { }

    [Function("PERMIT_TYPEHASH", "bytes32")]
    public class PermitTypehashFunctionBase : FunctionMessage
    {

    }

    public partial class ReceiveWithAuthorizationTypehashFunction : ReceiveWithAuthorizationTypehashFunctionBase { }

    [Function("RECEIVE_WITH_AUTHORIZATION_TYPEHASH", "bytes32")]
    public class ReceiveWithAuthorizationTypehashFunctionBase : FunctionMessage
    {

    }

    public partial class TransferWithAuthorizationTypehashFunction : TransferWithAuthorizationTypehashFunctionBase { }

    [Function("TRANSFER_WITH_AUTHORIZATION_TYPEHASH", "bytes32")]
    public class TransferWithAuthorizationTypehashFunctionBase : FunctionMessage
    {

    }

    public partial class AllowanceFunction : AllowanceFunctionBase { }

    [Function("allowance", "uint256")]
    public class AllowanceFunctionBase : FunctionMessage
    {
        [Parameter("address", "owner", 1)]
        public virtual string Owner { get; set; }
        [Parameter("address", "spender", 2)]
        public virtual string Spender { get; set; }
    }

    public partial class ApproveFunction : ApproveFunctionBase { }

    [Function("approve", "bool")]
    public class ApproveFunctionBase : FunctionMessage
    {
        [Parameter("address", "spender", 1)]
        public virtual string Spender { get; set; }
        [Parameter("uint256", "value", 2)]
        public virtual BigInteger Value { get; set; }
    }

    public partial class AuthorizationStateFunction : AuthorizationStateFunctionBase { }

    [Function("authorizationState", "bool")]
    public class AuthorizationStateFunctionBase : FunctionMessage
    {
        [Parameter("address", "authorizer", 1)]
        public virtual string Authorizer { get; set; }
        [Parameter("bytes32", "nonce", 2)]
        public virtual byte[] Nonce { get; set; }
    }

    public partial class BalanceOfFunction : BalanceOfFunctionBase { }

    [Function("balanceOf", "uint256")]
    public class BalanceOfFunctionBase : FunctionMessage
    {
        [Parameter("address", "account", 1)]
        public virtual string Account { get; set; }
    }

    public partial class DecimalsFunction : DecimalsFunctionBase { }

    [Function("decimals", "uint8")]
    public class DecimalsFunctionBase : FunctionMessage
    {

    }

    public partial class Eip712DomainFunction : Eip712DomainFunctionBase { }

    [Function("eip712Domain", typeof(Eip712DomainOutputDTO))]
    public class Eip712DomainFunctionBase : FunctionMessage
    {

    }

    public partial class MintFunction : MintFunctionBase { }

    [Function("mint")]
    public class MintFunctionBase : FunctionMessage
    {
        [Parameter("address", "to", 1)]
        public virtual string To { get; set; }
        [Parameter("uint256", "amount", 2)]
        public virtual BigInteger Amount { get; set; }
    }

    public partial class NameFunction : NameFunctionBase { }

    [Function("name", "string")]
    public class NameFunctionBase : FunctionMessage
    {

    }

    public partial class NoncesFunction : NoncesFunctionBase { }

    [Function("nonces", "uint256")]
    public class NoncesFunctionBase : FunctionMessage
    {
        [Parameter("address", "owner", 1)]
        public virtual string Owner { get; set; }
    }

    public partial class PermitFunction : PermitFunctionBase { }

    [Function("permit")]
    public class PermitFunctionBase : FunctionMessage
    {
        [Parameter("address", "owner", 1)]
        public virtual string Owner { get; set; }
        [Parameter("address", "spender", 2)]
        public virtual string Spender { get; set; }
        [Parameter("uint256", "value", 3)]
        public virtual BigInteger Value { get; set; }
        [Parameter("uint256", "deadline", 4)]
        public virtual BigInteger Deadline { get; set; }
        [Parameter("uint8", "v", 5)]
        public virtual byte V { get; set; }
        [Parameter("bytes32", "r", 6)]
        public virtual byte[] R { get; set; }
        [Parameter("bytes32", "s", 7)]
        public virtual byte[] S { get; set; }
    }

    public partial class ReceiveWithAuthorizationFunction : ReceiveWithAuthorizationFunctionBase { }

    [Function("receiveWithAuthorization")]
    public class ReceiveWithAuthorizationFunctionBase : FunctionMessage
    {
        [Parameter("address", "from", 1)]
        public virtual string From { get; set; }
        [Parameter("address", "to", 2)]
        public virtual string To { get; set; }
        [Parameter("uint256", "value", 3)]
        public virtual BigInteger Value { get; set; }
        [Parameter("uint256", "validAfter", 4)]
        public virtual BigInteger ValidAfter { get; set; }
        [Parameter("uint256", "validBefore", 5)]
        public virtual BigInteger ValidBefore { get; set; }
        [Parameter("bytes32", "nonce", 6)]
        public virtual byte[] Nonce { get; set; }
        [Parameter("uint8", "v", 7)]
        public virtual byte V { get; set; }
        [Parameter("bytes32", "r", 8)]
        public virtual byte[] R { get; set; }
        [Parameter("bytes32", "s", 9)]
        public virtual byte[] S { get; set; }
    }

    public partial class SymbolFunction : SymbolFunctionBase { }

    [Function("symbol", "string")]
    public class SymbolFunctionBase : FunctionMessage
    {

    }

    public partial class TotalSupplyFunction : TotalSupplyFunctionBase { }

    [Function("totalSupply", "uint256")]
    public class TotalSupplyFunctionBase : FunctionMessage
    {

    }

    public partial class TransferFunction : TransferFunctionBase { }

    [Function("transfer", "bool")]
    public class TransferFunctionBase : FunctionMessage
    {
        [Parameter("address", "to", 1)]
        public virtual string To { get; set; }
        [Parameter("uint256", "value", 2)]
        public virtual BigInteger Value { get; set; }
    }

    public partial class TransferFromFunction : TransferFromFunctionBase { }

    [Function("transferFrom", "bool")]
    public class TransferFromFunctionBase : FunctionMessage
    {
        [Parameter("address", "from", 1)]
        public virtual string From { get; set; }
        [Parameter("address", "to", 2)]
        public virtual string To { get; set; }
        [Parameter("uint256", "value", 3)]
        public virtual BigInteger Value { get; set; }
    }

    public partial class TransferWithAuthorizationFunction : TransferWithAuthorizationFunctionBase { }

    [Function("transferWithAuthorization")]
    public class TransferWithAuthorizationFunctionBase : FunctionMessage
    {
        [Parameter("address", "from", 1)]
        public virtual string From { get; set; }
        [Parameter("address", "to", 2)]
        public virtual string To { get; set; }
        [Parameter("uint256", "value", 3)]
        public virtual BigInteger Value { get; set; }
        [Parameter("uint256", "validAfter", 4)]
        public virtual BigInteger ValidAfter { get; set; }
        [Parameter("uint256", "validBefore", 5)]
        public virtual BigInteger ValidBefore { get; set; }
        [Parameter("bytes32", "nonce", 6)]
        public virtual byte[] Nonce { get; set; }
        [Parameter("uint8", "v", 7)]
        public virtual byte V { get; set; }
        [Parameter("bytes32", "r", 8)]
        public virtual byte[] R { get; set; }
        [Parameter("bytes32", "s", 9)]
        public virtual byte[] S { get; set; }
    }

    public partial class VersionFunction : VersionFunctionBase { }

    [Function("version", "string")]
    public class VersionFunctionBase : FunctionMessage
    {

    }

    public partial class ApprovalEventDTO : ApprovalEventDTOBase { }

    [Event("Approval")]
    public class ApprovalEventDTOBase : IEventDTO
    {
        [Parameter("address", "owner", 1, true )]
        public virtual string Owner { get; set; }
        [Parameter("address", "spender", 2, true )]
        public virtual string Spender { get; set; }
        [Parameter("uint256", "value", 3, false )]
        public virtual BigInteger Value { get; set; }
    }

    public partial class AuthorizationUsedEventDTO : AuthorizationUsedEventDTOBase { }

    [Event("AuthorizationUsed")]
    public class AuthorizationUsedEventDTOBase : IEventDTO
    {
        [Parameter("address", "authorizer", 1, true )]
        public virtual string Authorizer { get; set; }
        [Parameter("bytes32", "nonce", 2, true )]
        public virtual byte[] Nonce { get; set; }
    }

    public partial class EIP712DomainChangedEventDTO : EIP712DomainChangedEventDTOBase { }

    [Event("EIP712DomainChanged")]
    public class EIP712DomainChangedEventDTOBase : IEventDTO
    {
    }

    public partial class TransferEventDTO : TransferEventDTOBase { }

    [Event("Transfer")]
    public class TransferEventDTOBase : IEventDTO
    {
        [Parameter("address", "from", 1, true )]
        public virtual string From { get; set; }
        [Parameter("address", "to", 2, true )]
        public virtual string To { get; set; }
        [Parameter("uint256", "value", 3, false )]
        public virtual BigInteger Value { get; set; }
    }

    public partial class AuthorizationAlreadyUsedError : AuthorizationAlreadyUsedErrorBase { }
    [Error("AuthorizationAlreadyUsed")]
    public class AuthorizationAlreadyUsedErrorBase : IErrorDTO
    {
    }

    public partial class AuthorizationExpiredError : AuthorizationExpiredErrorBase { }
    [Error("AuthorizationExpired")]
    public class AuthorizationExpiredErrorBase : IErrorDTO
    {
    }

    public partial class AuthorizationNotYetValidError : AuthorizationNotYetValidErrorBase { }
    [Error("AuthorizationNotYetValid")]
    public class AuthorizationNotYetValidErrorBase : IErrorDTO
    {
    }

    public partial class CallerMustBePayeeError : CallerMustBePayeeErrorBase { }
    [Error("CallerMustBePayee")]
    public class CallerMustBePayeeErrorBase : IErrorDTO
    {
    }

    public partial class ECDSAInvalidSignatureError : ECDSAInvalidSignatureErrorBase { }
    [Error("ECDSAInvalidSignature")]
    public class ECDSAInvalidSignatureErrorBase : IErrorDTO
    {
    }

    public partial class ECDSAInvalidSignatureLengthError : ECDSAInvalidSignatureLengthErrorBase { }

    [Error("ECDSAInvalidSignatureLength")]
    public class ECDSAInvalidSignatureLengthErrorBase : IErrorDTO
    {
        [Parameter("uint256", "length", 1)]
        public virtual BigInteger Length { get; set; }
    }

    public partial class ECDSAInvalidSignatureSError : ECDSAInvalidSignatureSErrorBase { }

    [Error("ECDSAInvalidSignatureS")]
    public class ECDSAInvalidSignatureSErrorBase : IErrorDTO
    {
        [Parameter("bytes32", "s", 1)]
        public virtual byte[] S { get; set; }
    }

    public partial class ERC20InsufficientAllowanceError : ERC20InsufficientAllowanceErrorBase { }

    [Error("ERC20InsufficientAllowance")]
    public class ERC20InsufficientAllowanceErrorBase : IErrorDTO
    {
        [Parameter("address", "spender", 1)]
        public virtual string Spender { get; set; }
        [Parameter("uint256", "allowance", 2)]
        public virtual BigInteger Allowance { get; set; }
        [Parameter("uint256", "needed", 3)]
        public virtual BigInteger Needed { get; set; }
    }

    public partial class ERC20InsufficientBalanceError : ERC20InsufficientBalanceErrorBase { }

    [Error("ERC20InsufficientBalance")]
    public class ERC20InsufficientBalanceErrorBase : IErrorDTO
    {
        [Parameter("address", "sender", 1)]
        public virtual string Sender { get; set; }
        [Parameter("uint256", "balance", 2)]
        public virtual BigInteger Balance { get; set; }
        [Parameter("uint256", "needed", 3)]
        public virtual BigInteger Needed { get; set; }
    }

    public partial class ERC20InvalidApproverError : ERC20InvalidApproverErrorBase { }

    [Error("ERC20InvalidApprover")]
    public class ERC20InvalidApproverErrorBase : IErrorDTO
    {
        [Parameter("address", "approver", 1)]
        public virtual string Approver { get; set; }
    }

    public partial class ERC20InvalidReceiverError : ERC20InvalidReceiverErrorBase { }

    [Error("ERC20InvalidReceiver")]
    public class ERC20InvalidReceiverErrorBase : IErrorDTO
    {
        [Parameter("address", "receiver", 1)]
        public virtual string Receiver { get; set; }
    }

    public partial class ERC20InvalidSenderError : ERC20InvalidSenderErrorBase { }

    [Error("ERC20InvalidSender")]
    public class ERC20InvalidSenderErrorBase : IErrorDTO
    {
        [Parameter("address", "sender", 1)]
        public virtual string Sender { get; set; }
    }

    public partial class ERC20InvalidSpenderError : ERC20InvalidSpenderErrorBase { }

    [Error("ERC20InvalidSpender")]
    public class ERC20InvalidSpenderErrorBase : IErrorDTO
    {
        [Parameter("address", "spender", 1)]
        public virtual string Spender { get; set; }
    }

    public partial class FaucetLimitExceededError : FaucetLimitExceededErrorBase { }
    [Error("FaucetLimitExceeded")]
    public class FaucetLimitExceededErrorBase : IErrorDTO
    {
    }

    public partial class InvalidShortStringError : InvalidShortStringErrorBase { }
    [Error("InvalidShortString")]
    public class InvalidShortStringErrorBase : IErrorDTO
    {
    }

    public partial class InvalidSignatureError : InvalidSignatureErrorBase { }
    [Error("InvalidSignature")]
    public class InvalidSignatureErrorBase : IErrorDTO
    {
    }

    public partial class PermitExpiredError : PermitExpiredErrorBase { }
    [Error("PermitExpired")]
    public class PermitExpiredErrorBase : IErrorDTO
    {
    }

    public partial class StringTooLongError : StringTooLongErrorBase { }

    [Error("StringTooLong")]
    public class StringTooLongErrorBase : IErrorDTO
    {
        [Parameter("string", "str", 1)]
        public virtual string Str { get; set; }
    }

    public partial class DomainSeparatorOutputDTO : DomainSeparatorOutputDTOBase { }

    [FunctionOutput]
    public class DomainSeparatorOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bytes32", "", 1)]
        public virtual byte[] ReturnValue1 { get; set; }
    }

    public partial class FaucetLimitOutputDTO : FaucetLimitOutputDTOBase { }

    [FunctionOutput]
    public class FaucetLimitOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }

    public partial class PermitTypehashOutputDTO : PermitTypehashOutputDTOBase { }

    [FunctionOutput]
    public class PermitTypehashOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bytes32", "", 1)]
        public virtual byte[] ReturnValue1 { get; set; }
    }

    public partial class ReceiveWithAuthorizationTypehashOutputDTO : ReceiveWithAuthorizationTypehashOutputDTOBase { }

    [FunctionOutput]
    public class ReceiveWithAuthorizationTypehashOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bytes32", "", 1)]
        public virtual byte[] ReturnValue1 { get; set; }
    }

    public partial class TransferWithAuthorizationTypehashOutputDTO : TransferWithAuthorizationTypehashOutputDTOBase { }

    [FunctionOutput]
    public class TransferWithAuthorizationTypehashOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bytes32", "", 1)]
        public virtual byte[] ReturnValue1 { get; set; }
    }

    public partial class AllowanceOutputDTO : AllowanceOutputDTOBase { }

    [FunctionOutput]
    public class AllowanceOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }



    public partial class AuthorizationStateOutputDTO : AuthorizationStateOutputDTOBase { }

    [FunctionOutput]
    public class AuthorizationStateOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bool", "", 1)]
        public virtual bool ReturnValue1 { get; set; }
    }

    public partial class BalanceOfOutputDTO : BalanceOfOutputDTOBase { }

    [FunctionOutput]
    public class BalanceOfOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }

    public partial class DecimalsOutputDTO : DecimalsOutputDTOBase { }

    [FunctionOutput]
    public class DecimalsOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint8", "", 1)]
        public virtual byte ReturnValue1 { get; set; }
    }

    public partial class Eip712DomainOutputDTO : Eip712DomainOutputDTOBase { }

    [FunctionOutput]
    public class Eip712DomainOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("bytes1", "fields", 1)]
        public virtual byte[] Fields { get; set; }
        [Parameter("string", "name", 2)]
        public virtual string Name { get; set; }
        [Parameter("string", "version", 3)]
        public virtual string Version { get; set; }
        [Parameter("uint256", "chainId", 4)]
        public virtual BigInteger ChainId { get; set; }
        [Parameter("address", "verifyingContract", 5)]
        public virtual string VerifyingContract { get; set; }
        [Parameter("bytes32", "salt", 6)]
        public virtual byte[] Salt { get; set; }
        [Parameter("uint256[]", "extensions", 7)]
        public virtual List<BigInteger> Extensions { get; set; }
    }



    public partial class NameOutputDTO : NameOutputDTOBase { }

    [FunctionOutput]
    public class NameOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("string", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }

    public partial class NoncesOutputDTO : NoncesOutputDTOBase { }

    [FunctionOutput]
    public class NoncesOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }





    public partial class SymbolOutputDTO : SymbolOutputDTOBase { }

    [FunctionOutput]
    public class SymbolOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("string", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }

    public partial class TotalSupplyOutputDTO : TotalSupplyOutputDTOBase { }

    [FunctionOutput]
    public class TotalSupplyOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }







    public partial class VersionOutputDTO : VersionOutputDTOBase { }

    [FunctionOutput]
    public class VersionOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("string", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }
}
