#!/bin/bash

# SQLDBEntityNotifier Multi-Database CDC Tests Runner
# This script runs all tests for the enhanced CDC functionality

echo "🚀 Starting SQLDBEntityNotifier Multi-Database CDC Tests"
echo "=================================================="

# Set colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if we're in the right directory
if [ ! -f "SQLDBEntityNotifier.Tests.csproj" ]; then
    print_error "Please run this script from the SQLDBEntityNotifier.Tests directory"
    exit 1
fi

# Check if .NET 6.0 is available
if ! command -v dotnet &> /dev/null; then
    print_error ".NET 6.0 SDK is not installed or not in PATH"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
print_status "Using .NET version: $DOTNET_VERSION"

# Clean previous builds
print_status "Cleaning previous builds..."
dotnet clean --verbosity quiet
if [ $? -eq 0 ]; then
    print_success "Clean completed"
else
    print_warning "Clean had warnings"
fi

# Restore packages
print_status "Restoring NuGet packages..."
dotnet restore --verbosity quiet
if [ $? -eq 0 ]; then
    print_success "Package restore completed"
else
    print_error "Package restore failed"
    exit 1
fi

# Build the project
print_status "Building test project..."
dotnet build --verbosity quiet --no-restore
if [ $? -eq 0 ]; then
    print_success "Build completed"
else
    print_error "Build failed"
    exit 1
fi

echo ""
echo "🧪 Running Tests by Category"
echo "============================"

# Run all tests
print_status "Running all tests..."
dotnet test --verbosity normal --no-build --logger "console;verbosity=normal"

# Store the exit code
TEST_EXIT_CODE=$?

echo ""
echo "📊 Test Results Summary"
echo "======================"

if [ $TEST_EXIT_CODE -eq 0 ]; then
    print_success "All tests passed! 🎉"
    echo ""
    echo "✅ Test Coverage Includes:"
    echo "   • SQL Server CDC Provider"
    echo "   • MySQL CDC Provider" 
    echo "   • PostgreSQL CDC Provider"
    echo "   • CDC Provider Factory"
    echo "   • Unified Notification Service"
    echo "   • Database Configuration"
    echo "   • Enhanced Event Arguments"
    echo "   • Backward Compatibility"
    echo "   • Model Validation"
else
    print_error "Some tests failed! ❌"
    echo ""
    echo "🔍 Check the test output above for details"
fi

echo ""
echo "📁 Test Files Created:"
echo "======================"

# List test files
find . -name "*.cs" -type f | grep -E "(Tests|Test)" | sort | while read -r file; do
    filename=$(basename "$file")
    echo "   • $filename"
done

echo ""
echo "🚀 Next Steps:"
echo "=============="
echo "1. Review test results above"
echo "2. Check specific test failures if any"
echo "3. Run individual test categories:"
echo "   • dotnet test --filter \"FullyQualifiedName~SqlServerCDCProvider\""
echo "   • dotnet test --filter \"FullyQualifiedName~MySqlCDCProvider\""
echo "   • dotnet test --filter \"FullyQualifiedName~PostgreSqlCDCProvider\""
echo "   • dotnet test --filter \"FullyQualifiedName~BackwardCompatibility\""
echo "4. Run with detailed output: dotnet test --verbosity detailed"
echo "5. Generate coverage report: dotnet test --collect:\"XPlat Code Coverage\""

echo ""
echo "🏁 Test execution completed with exit code: $TEST_EXIT_CODE"

exit $TEST_EXIT_CODE