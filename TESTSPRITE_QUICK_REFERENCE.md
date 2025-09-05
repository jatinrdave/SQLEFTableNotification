# TestSprite Quick Reference Guide
## SQLDBEntityNotifier v2.0 - Testing Commands & Usage

---

## 🚀 **Quick Start Commands**

### **1. Bootstrap TestSprite**
```bash
# Initialize TestSprite with project configuration
mcp_TestSpritenew_testsprite_bootstrap_tests
```
**Parameters:**
- `localPort`: 5173 (or your service port)
- `type`: "backend" (for .NET library)
- `projectPath`: Full path to project root
- `testScope`: "codebase" (for full project testing)

### **2. Generate Code Summary**
```bash
# Analyze project repository and summarize codebase
mcp_TestSpritenew_testsprite_generate_code_summary
```
**Parameters:**
- `projectRootPath`: Full path to project root

### **3. Generate Standardized PRD**
```bash
# Create structured Product Requirements Document
mcp_TestSpritenew_testsprite_generate_standardized_prd
```
**Parameters:**
- `projectPath`: Full path to project root

---

## 📋 **Test Plan Generation**

### **4. Generate Backend Test Plan**
```bash
# Create comprehensive backend testing strategy
mcp_TestSpritenew_testsprite_generate_backend_test_plan
```
**Parameters:**
- `projectPath`: Full path to project root

### **5. Generate Frontend Test Plan**
```bash
# Create frontend testing strategy (if applicable)
mcp_TestSpritenew_testsprite_generate_frontend_test_plan
```
**Parameters:**
- `projectPath`: Full path to project root
- `needLogin`: true/false (default: true)

---

## 🧪 **Test Execution**

### **6. Generate and Execute Tests**
```bash
# Run comprehensive test suite with AI analysis
mcp_TestSpritenew_testsprite_generate_code_and_execute
```
**Parameters:**
- `projectName`: "SQLEFTableNotification" (root directory name)
- `projectPath`: Full path to project root
- `testIds`: [] (empty array for all tests, or specific test IDs)
- `additionalInstruction`: Custom testing instructions

---

## ⚙️ **Configuration Parameters**

### **Local Port Configuration**
- **Default**: 5173
- **Alternative**: 44342 (from launchSettings.json)
- **Check**: Use `netstat -an | findstr :5173` to verify

### **Project Type**
- **Value**: "backend"
- **Reason**: .NET library with no UI components

### **Test Scope**
- **Value**: "codebase"
- **Reason**: Comprehensive testing of entire project

---

## 📁 **Project Structure for TestSprite**

```
SQLEFTableNotification/
├── SQLDBEntityNotifier/           # Core library
│   ├── Interfaces/                # Core contracts
│   ├── Models/                    # Data models
│   ├── Providers/                 # CDC providers
│   └── UnifiedDBNotificationService.cs
├── SQLDBEntityNotifier.Tests/     # Main test suite
├── SQLEFTableNotification/        # Domain models
├── SQLEFTableNotificationLib/     # Library implementation
└── SQLEFTableNotification.Console/ # Console app
```

---

## 🔧 **Common TestSprite Workflows**

### **Workflow 1: Initial Setup**
```bash
# 1. Bootstrap TestSprite
mcp_TestSpritenew_testsprite_bootstrap_tests

# 2. Generate code summary
mcp_TestSpritenew_testsprite_generate_code_summary

# 3. Generate PRD
mcp_TestSpritenew_testsprite_generate_standardized_prd
```

### **Workflow 2: Test Planning**
```bash
# 1. Generate backend test plan
mcp_TestSpritenew_testsprite_generate_backend_test_plan

# 2. Review and customize test plan
# 3. Execute comprehensive testing
```

### **Workflow 3: Test Execution**
```bash
# 1. Run all tests
mcp_TestSpritenew_testsprite_generate_code_and_execute

# 2. Run specific test categories
mcp_TestSpritenew_testsprite_generate_code_and_execute
# With additionalInstruction: "Focus on CDC provider tests"

# 3. Run performance tests
mcp_TestSpritenew_testsprite_generate_code_and_execute
# With additionalInstruction: "Focus on performance and stress testing"
```

---

## 🎯 **Testing Focus Areas**

### **Core CDC Testing**
- Multi-database support (SQL Server, MySQL, PostgreSQL)
- Change detection accuracy
- Column filtering functionality
- Event notification system

### **Advanced Features Testing**
- Change analytics and metrics
- Schema change detection
- Change correlation engine
- Context management

### **Performance Testing**
- Latency benchmarks (<100ms)
- Throughput testing (1000+ changes/sec)
- Memory usage optimization
- CPU utilization control

### **Security Testing**
- Connection security
- Authentication/authorization
- Data protection
- Audit logging

---

## 🚨 **Troubleshooting**

### **Common Issues**

#### **1. Connection Failed**
```bash
# Check if service is running on port 5173
netstat -an | findstr :5173

# Alternative: Check port 44342
netstat -an | findstr :44342
```

#### **2. MCP Configuration Issues**
- Verify `mcp.json` configuration
- Check API key validity
- Ensure TestSprite command is accessible

#### **3. Project Path Issues**
- Use absolute paths
- Verify project structure
- Check file permissions

### **Fallback Options**
- Manual test execution with `dotnet test`
- Use existing test suite (379 tests)
- Focus on specific test categories

---

## 📊 **Expected Test Results**

### **Current Status**
- **Total Tests**: 379
- **Passing**: 379
- **Failing**: 0
- **Coverage**: >90%

### **TestSprite Goals**
- **New Test Coverage**: Advanced features
- **Performance Validation**: Benchmarks and stress tests
- **Security Assessment**: Vulnerability identification
- **Integration Testing**: Real database scenarios

---

## 📞 **Support Resources**

### **TestSprite Documentation**
- **Official Site**: https://www.testsprite.com/
- **MCP Integration**: Model Context Protocol
- **API Reference**: Available through MCP tools

### **Project Resources**
- **Repository**: https://github.com/jatinrdave/SQLEFTableNotification
- **NuGet Package**: SQLDBEntityNotifier v2.0.0
- **Documentation**: Comprehensive README files

---

## 🎉 **Success Checklist**

- [ ] TestSprite successfully bootstrapped
- [ ] Code analysis completed
- [ ] Test plan generated
- [ ] All existing tests pass (379/379)
- [ ] New test coverage added
- [ ] Performance benchmarks met
- [ ] Security assessment completed
- [ ] Integration testing validated
- [ ] Final report generated

---

*This quick reference guide provides immediate access to all TestSprite commands and workflows needed for comprehensive testing of the SQLDBEntityNotifier project.*

